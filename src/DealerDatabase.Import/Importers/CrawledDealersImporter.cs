using System.Globalization;
using CsvHelper.Configuration.Attributes;
using Microsoft.Extensions.Logging;

namespace DealerDatabase.Import.Importers;

/// <summary>
/// Imports successful website crawl results and applies their dealer details.
/// </summary>
internal sealed class CrawledDealersImporter(DealerImportSession session, ILogger logger)
{
    internal Task ImportAsync(string dataDir, CancellationToken cancellationToken)
    {
        var path = Path.Combine(dataDir, "crawled_dealers.csv");
        if (!File.Exists(path))
        {
            logger.LogWarning("Crawled-dealers input not found: {Path}", path);
            return Task.CompletedTask;
        }

        foreach (var record in ImportParsing.ReadCsv<CrawledDealerRecord>(path)
                     .OrderBy(item => item.CrawledAt, StringComparer.Ordinal)
                     .ThenBy(item => item.CrawlId, StringComparer.OrdinalIgnoreCase))
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!int.TryParse(record.HttpStatus, NumberStyles.Integer, CultureInfo.InvariantCulture, out var statusCode) || statusCode != 200)
            {
                continue;
            }

            var recordId = ImportParsing.EmptyToNull(record.CrawlId);
            if (recordId is null)
            {
                continue;
            }

            var crn = Normalization.NormalizeCrn(record.CompanyNumberDetected);
            var frn = ImportParsing.EmptyToNull(record.FcaFrnDetected);
            var name = ImportParsing.EmptyToNull(record.BusinessNameDetected);
            var postcode = Normalization.NormalizePostcode(record.PostcodeDetected) ??
                ImportParsing.NormalizePostcodeFromAddress(record.AddressDetected);

            // Prefer stable registry identifiers; use the name/postcode fallback only when no identifier matches.
            var dealer = session.FindByCrn(crn);
            if (dealer is null && frn is not null)
            {
                dealer = session.FindByFrn(frn);
            }

            dealer ??= session.MatchBySoftKey(name, postcode);
            if (dealer is null && crn is not null && name is not null)
            {
                dealer = session.GetOrCreateByCrn(crn, "Crawler", recordId, name);
            }

            if (dealer is null)
            {
                continue;
            }

            if (dealer.CompanyRegistrationNumber is null && crn is not null)
            {
                dealer.CompanyRegistrationNumber = crn;
                session.RegisterCrn(dealer);
            }

            dealer.FcaReferenceNumber ??= frn;
            session.RegisterFrn(dealer);
            var vatNumber = ImportParsing.EmptyToNull(record.VatNumberDetected);
            var stockCount = ImportParsing.ParseInt(record.StockCountDetected);
            var telephone = Normalization.NormalizePhone(ImportParsing.FirstContactValue(record.PhonesDetected));
            var email = ImportParsing.FirstContactValue(record.EmailsDetected);
            var finalWebsite = Normalization.NormalizeWebsite(record.FinalUrl);
            var sourceWebsite = Normalization.NormalizeWebsite(record.SourceUrl);
            var website = finalWebsite ?? sourceWebsite;
            var tradingAddress = Normalization.NormalizeAddress(record.AddressDetected);

            dealer.VatNumber = vatNumber ?? dealer.VatNumber;
            dealer.StockCount = stockCount ?? dealer.StockCount;
            dealer.Telephone = telephone ?? dealer.Telephone;
            dealer.Email = email ?? dealer.Email;
            dealer.Website = website ?? dealer.Website;
            dealer.TradingAddress ??= tradingAddress;
            var sourceRecord = session.AddSourceRecord(dealer, "Crawler", recordId,
                ImportParsing.SourceField("crawl_id", record.CrawlId),
                ImportParsing.SourceField("crawled_at", record.CrawledAt),
                ImportParsing.SourceField("source_url", record.SourceUrl, sourceWebsite),
                ImportParsing.SourceField("final_url", record.FinalUrl, finalWebsite, "Website",
                    isCurrentValue: finalWebsite is not null && dealer.Website == finalWebsite),
                ImportParsing.SourceField("http_status", record.HttpStatus, statusCode),
                ImportParsing.SourceField("company_number_detected", record.CompanyNumberDetected, crn, "CompanyRegistrationNumber",
                    isCurrentValue: crn is not null && dealer.CompanyRegistrationNumber == crn),
                ImportParsing.SourceField("fca_frn_detected", record.FcaFrnDetected, frn, "FcaReferenceNumber",
                    isCurrentValue: frn is not null && dealer.FcaReferenceNumber == frn),
                ImportParsing.SourceField("business_name_detected", record.BusinessNameDetected, Normalization.NormalizeName(name)),
                ImportParsing.SourceField("postcode_detected", record.PostcodeDetected, postcode),
                ImportParsing.SourceField("vat_number_detected", record.VatNumberDetected, vatNumber, "VatNumber",
                    isCurrentValue: vatNumber is not null && dealer.VatNumber == vatNumber),
                ImportParsing.SourceField("stock_count_detected", record.StockCountDetected, stockCount, "StockCount",
                    isCurrentValue: stockCount.HasValue && dealer.StockCount == stockCount),
                ImportParsing.SourceField("phones_detected", record.PhonesDetected, telephone, "Telephone",
                    isCurrentValue: telephone is not null && dealer.Telephone == telephone),
                ImportParsing.SourceField("emails_detected", record.EmailsDetected, email, "Email",
                    isCurrentValue: email is not null && dealer.Email == email),
                ImportParsing.SourceField("address_detected", record.AddressDetected, tradingAddress, "TradingAddress",
                    isCurrentValue: tradingAddress is not null && dealer.TradingAddress == tradingAddress));

            session.AddNameObservation(dealer, sourceRecord, "Trading", "business_name_detected", 0,
                record.BusinessNameDetected);
            session.AddAddressObservation(dealer, sourceRecord, "Trading", "address_detected", 0,
                record.AddressDetected, tradingAddress,
                isPrimary: tradingAddress is not null && dealer.TradingAddress == tradingAddress);
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Maps a row from the crawled-dealers CSV file.
    /// </summary>
    private sealed class CrawledDealerRecord
    {
        [Name("crawl_id")]
        public string? CrawlId { get; set; }

        [Name("crawled_at")]
        public string? CrawledAt { get; set; }

        [Name("source_url")]
        public string? SourceUrl { get; set; }

        [Name("final_url")]
        public string? FinalUrl { get; set; }

        [Name("http_status")]
        public string? HttpStatus { get; set; }

        [Name("business_name_detected")]
        public string? BusinessNameDetected { get; set; }

        [Name("address_detected")]
        public string? AddressDetected { get; set; }

        [Name("postcode_detected")]
        public string? PostcodeDetected { get; set; }

        [Name("phones_detected")]
        public string? PhonesDetected { get; set; }

        [Name("emails_detected")]
        public string? EmailsDetected { get; set; }

        [Name("company_number_detected")]
        public string? CompanyNumberDetected { get; set; }

        [Name("vat_number_detected")]
        public string? VatNumberDetected { get; set; }

        [Name("fca_frn_detected")]
        public string? FcaFrnDetected { get; set; }

        [Name("stock_count_detected")]
        public string? StockCountDetected { get; set; }
    }
}