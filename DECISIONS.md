# Dealership data consolidation decisions

## Matching order

The importer processes sources in a fixed order so later records can enrich dealers created or matched by earlier, higher-confidence sources:

1. **Companies House** is the anchor. `company_number` is normalized and used as the dealer key. The company name, status, creation date, registered address, and postcode populate the consolidated dealer.
2. **FCA** records link by Companies House number when available. Without a CRN, an existing dealer can be found by FRN; otherwise a dealer is created from `Organisation Name`. FRN is retained as a direct lookup key. The first non-empty trading name is preferred; `Organisation Name` is used as the trading name only when it differs from the dealer's legal name.
3. **ICO** records link only to an existing dealer by `Company_registration_number`. Rows with a missing/unmatched CRN are ignored rather than creating a separate dealer.
4. **SAF** first recognizes an existing SAF source-record key (for repeat imports), then uses an exact soft key: normalized legal name plus normalized postcode. If no dealer matches, a dealer is created from `LegalName` or, as in the sample, `Name`.
5. **Crawled dealers** prefer detected CRN, then detected FRN, then the exact name/postcode soft key. Only successful HTTP 200 rows are used. A new dealer is created for an unmatched CRN only when the crawl supplies a business name; otherwise an unmatched crawl is skipped.
6. **VAT lookup** and **Marketcheck** use the exact normalized name/postcode soft key. They enrich a matched dealer but do not create one when the key is missing or unmatched. A Marketcheck row must also have a non-empty `mc_dealer_id` so its source record can be identified; rows without one are skipped.

CRNs have a trailing `.0` removed, whitespace removed, are uppercased, and are left-padded to at least eight characters. Postcodes have whitespace removed and are uppercased. Names are lowercased, stripped of punctuation, have `ltd`, `limited`, `uk`, `co`, and `company` tokens removed, and have repeated spaces collapsed. Addresses collapse whitespace and standardize comma spacing. Unambiguous UK phone numbers are converted to `+44` form; values with extensions or ambiguous formats are preserved. Websites are parsed as HTTP(S), default to HTTPS when no scheme is given, and use a lowercase IDN host while preserving path and query. Soft matching requires both a non-empty normalized legal name and postcode and does not use fuzzy similarity.

## Import architecture

`DealerImportOrchestrator` owns the import lifecycle: it validates the input directory, loads the existing dealer graph and source observations, invokes the source importers in the order above, and saves once after all passes complete. The passes remain sequential because each source may match or enrich dealers created or updated by earlier sources; running them concurrently would change matching and conflict-resolution behavior. The console entry point applies pending EF Core migrations before starting the import.

Each source-specific importer owns its input file format, record model, and mapping into dealer fields and source observations. A single `DealerImportSession` is shared across the passes to maintain the CRN, FRN, and source-record indexes and to centralize matching and persistence of lineage, field values, names, and addresses. `ImportParsing` contains format-independent parsing and conversion helpers, while `Normalization` contains the canonical value rules described above. Source-specific rules stay in their importer; shared matching and observation behavior stays in the session.

The `Dealer` row holds the consolidated current values. Related source records retain per-input identity and processing metadata; source-field observations retain raw and normalized values, and name/address observations retain source-specific occurrences and primary flags. A field observation can be marked as the current source for a consolidated field, but this is not a time-series history of every prior winner. `ContributedFieldsJson` is a sorted, cumulative list of non-empty field names seen for that source record.

## Conflict resolution

- Companies House is authoritative for the legal name, company status, incorporation date, registered address, and registered postcode. FCA and SAF do not replace an existing Companies House legal name; SAF can create a dealer when no match exists.
- FCA supplies FCA reference/status and its first trading name (or differing organization name) before the SAF pass. A non-empty SAF `TradingAs` value then replaces an earlier trading name.
- SAF telephone, website, trading address, and postcode only fill fields that are currently null.
- Crawl rows with HTTP 200 are applied in `crawled_at` string order, then `crawl_id`; later non-empty phone, email, website, VAT-number, and stock-count values replace earlier crawl values. Crawl data does not set VAT validity. Trading address only fills a null value.
- VAT responses with a target and no response `code` are considered valid; a target VAT number replaces the current number when present. A response with a target is recorded even if its code indicates invalidity. Sample `NOT_FOUND` responses have no target, so they are not matched or recorded against a dealer.
- Marketcheck is processed after crawl data in `last_seen` string order, then dealer ID. Non-empty phone, website, email, and inventory count values replace the current values; its trading address only fills a null value.
- Dates are parsed using en-GB culture to accommodate the sample's ISO, day/month/year, and textual date formats. Unparseable optional dates remain null.

## Traceability and repeat imports

Each linked input contributes a `DealerSourceRecord` keyed by `(SourceSystem, SourceRecordId)`, with `ProcessedAt` set in UTC when the record is first created. Source-field, name, and address observations are retained with their originating record. Reprocessing a record updates its existing observations and unions its contributed-field names; it does not refresh the original `ProcessedAt`. The unique `(SourceSystem, SourceRecordId)` index prevents duplicate lineage records, and the unique nullable CRN index protects populated dealer identities. Deleting a dealer cascades to its source records and observations.

## Input assumptions

The importer uses the files and headers currently present under `data/`: `companies_house.json` has an `items` array; `fca_register.json` has a `Data` array with named properties such as `Organisation Name`; `ico_register.csv`, `crawled_dealers.csv`, and `marketcheck_dealers.csv` are CSV; `saf_members.xml` has `Member` elements; and JSON files under `vat_lookups/` have a `target` object and optional `code`. Crawler postcode is recovered from `address_detected` when `postcode_detected` is empty. Missing optional source files are logged and skipped; malformed present files fail the import rather than silently discarding data.

CRNs lose a trailing `.0`, have whitespace removed, are uppercased, and are padded to at least eight characters. Postcodes lose whitespace and are uppercased. Names are lowercased, punctuation is removed, the tokens `ltd`, `limited`, `uk`, `co`, and `company` are removed, and repeated spaces are collapsed. Address whitespace and comma spacing are standardized. Unambiguous UK phone numbers are converted to `+44`; numbers with extensions and values that cannot be normalized safely are preserved. Websites accept HTTP(S), assume HTTPS when no scheme is supplied, lowercase the IDN host, and retain path and query while dropping fragments. Dates use en-GB parsing; invalid optional dates remain null. These rules are deterministic, not fuzzy: a soft match requires both a non-empty normalized legal name and postcode.

The EF model changes are represented by migrations. The expanded dealer migration renames the legacy `Name` column to `LegalName` and preserves existing values; the later migration adds contributed-field metadata. Both the importer and optional web application apply pending migrations at startup and use `dealers.db` in the solution root. Use the README migration commands to apply pending migrations or scaffold future changes.

## Deferred work

- Fuzzy or probabilistic entity matching and a review queue for uncertain matches.
- A chronological per-field winner history and a source-priority model configurable outside code. Current raw/normalized field observations are retained, but not a complete history of value changes over time.
- Importing and modeling Companies House officers/directors and crawler finance-calculator details; the brief lists these as examples of additional source-specific data, but they are not currently consolidated.
- Import-run telemetry, richer reconciliation reports, and larger-volume performance testing.