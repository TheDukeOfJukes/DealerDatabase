# Dealer Database

A .NET 8 solution that consolidates fictional dealership data from seven sources into a SQLite database using Entity Framework Core. The importer normalizes values, matches records in a fixed precedence order, and retains source-record lineage and source-field, name, and address observations. The matching rules, conflict decisions, input assumptions, and deferred work are documented in [DECISIONS.md](DECISIONS.md).

## Prerequisites

- .NET 8 SDK
- Network access to nuget.org for package and local-tool restore

The repository-level `NuGet.Config` uses nuget.org because all solution dependencies are public. It prevents an unrelated inherited private feed from blocking clean restores. If private packages are added later, configure credentials through an approved credential provider or user-level configuration; do not commit tokens.

## Restore, build, and test

Run these commands from the solution root:

```powershell
dotnet restore DealerDatabase.sln --configfile NuGet.Config
dotnet tool restore --configfile NuGet.Config
dotnet build DealerDatabase.sln --no-restore
dotnet test DealerDatabase.sln --no-restore
```

The solution uses the `dotnet-ef` local tool manifest in `.config/dotnet-tools.json`.

## Import the sample data

Run the importer from the solution root so it can locate the `data/` directory and shared `dealers.db` file:

```powershell
dotnet run --project src/DealerDatabase.Import
```

The input files cover Companies House, FCA, ICO, SAF, crawled dealers, VAT lookups, and Marketcheck. The importer applies pending database migrations and is designed to be safe to rerun: existing source records are updated rather than duplicated. Import passes run sequentially so later sources can match or enrich dealers established by earlier sources. Missing optional input files are logged and skipped; malformed files that are present fail the import.

The source order is Companies House, FCA, ICO, SAF, crawled dealers, VAT lookups, then Marketcheck. Matching uses registry identifiers where available and otherwise an exact normalized legal-name/postcode pair for the applicable sources; it does not use fuzzy matching. See [DECISIONS.md](DECISIONS.md) for the per-source rules and conflict behavior.

## Database migrations

The importer and web application apply pending migrations when they start. To apply migrations explicitly from the solution root, use:

```powershell
dotnet ef database update --project src/DealerDatabase.Data
```

After changing the EF model, create a migration with:

```powershell
dotnet ef migrations add <MigrationName> --project src/DealerDatabase.Data
```

The importer and web project share `dealers.db` in the solution root. The data directory and database path are resolved relative to the solution root. Back up any database you need before replacing or deleting it.

## Optional web project

The optional ASP.NET Core MVC web application displays a read-only dealer list ordered by legal name. It applies pending migrations at startup and uses the same solution-root database as the importer:

```powershell
dotnet run --project src/DealerDatabase.Web
```

## Project layout

- `src/DealerDatabase.Data` - dealer and source-observation entities, SQLite `DealerDbContext`, solution-root paths, and EF Core migrations
- `src/DealerDatabase.Import/Program.cs` - console entry point; applies migrations and starts the import
- `src/DealerDatabase.Import/Importers/DealerImportOrchestrator.cs` - validates inputs, loads the dealer graph, runs importers in precedence order, and saves once
- `src/DealerDatabase.Import/Importers/` - Companies House, FCA, ICO, SAF, crawler, and VAT source importers
- `src/DealerDatabase.Import/MarketcheckImporter.cs` - Marketcheck CSV importer
- `src/DealerDatabase.Import/DealerImportSession.cs` - per-run indexes, matching, and persistence of source records and observations
- `src/DealerDatabase.Import/ImportParsing.cs` and `Normalization.cs` - shared parsing/conversion and canonical normalization rules
- `src/DealerDatabase.Web` - optional read-only ASP.NET Core MVC dealer list
- `tests/DealerDatabase.Data.Tests`, `tests/DealerDatabase.Import.Tests`, and `tests/DealerDatabase.Web.Tests` - persistence/migration, import, and web tests
- `data/` - fictional source data files
- `DECISIONS.md` - matching, conflict-resolution, input assumptions, and deferred work

The consolidated dealer model includes legal and trading names, company registration/status and incorporation date, registered and trading addresses, contact details, FCA/ICO/SAF details, VAT number and validity, and stock count. Source records retain field values and name/address observations with their source identity. Companies House officer/director details and crawler finance-calculator details are not currently imported; other deferred scope is listed in `DECISIONS.md`.
