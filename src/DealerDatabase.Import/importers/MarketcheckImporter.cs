using CsvHelper.Configuration.Attributes;
using Microsoft.Extensions.Logging;

namespace DealerDatabase.Import;

/// <summary>
/// Imports Marketcheck dealer listings and enriches matched dealer records.
/// </summary>
internal sealed class MarketcheckImporter(DealerImportSession session, ILogger logger)
{
    internal Task ImportAsync(string dataDir, CancellationToken cancellationToken)
    {
        var path = Path.Combine(dataDir, "marketcheck_dealers.csv");
        if (!File.Exists(path))
        {
            logger.LogWarning("Marketcheck input not found: {Path}", path);
            return Task.CompletedTask;
        }

        foreach (var record in ImportParsing.ReadCsv<MarketcheckDealerRecord>(path)
                     .OrderBy(item => item.LastSeen, StringComparer.Ordinal)
                     .ThenBy(item => item.DealerId, StringComparer.OrdinalIgnoreCase))
        {
            cancellationToken.ThrowIfCancellationRequested();

            var dealerId = ImportParsing.EmptyToNull(record.DealerId);
            var dealer = session.MatchBySoftKey(record.SellerName, record.Postcode);
            if (dealerId is null || dealer is null)
            {
                continue;
            }

            var inventoryCount = ImportParsing.ParseInt(record.InventoryCount);
            var phone = Normalization.NormalizePhone(record.Phone);
            var website = Normalization.NormalizeWebsite(record.Website);
            var email = ImportParsing.EmptyToNull(record.Email);
            var rawAddress = ImportParsing.FormatRawAddress(record.Street, record.City, record.County, record.Postcode);
            var tradingAddress = ImportParsing.FormatAddress(record.Street, record.City, record.County, record.Postcode);
            dealer.StockCount = inventoryCount ?? dealer.StockCount;
            dealer.Telephone = phone ?? dealer.Telephone;
            dealer.Website = website ?? dealer.Website;
            dealer.Email = email ?? dealer.Email;
            dealer.TradingAddress ??= tradingAddress;
            var sourceRecord = session.AddSourceRecord(dealer, "Marketcheck", dealerId,
                ImportParsing.SourceField("seller_name", record.SellerName, Normalization.NormalizeName(record.SellerName)),
                ImportParsing.SourceField("street", record.Street),
                ImportParsing.SourceField("city", record.City),
                ImportParsing.SourceField("county", record.County),
                ImportParsing.SourceField("postcode", record.Postcode, Normalization.NormalizePostcode(record.Postcode)),
                ImportParsing.SourceField("phone", record.Phone, phone, "Telephone", isCurrentValue: phone is not null && dealer.Telephone == phone),
                ImportParsing.SourceField("website", record.Website, website, "Website", isCurrentValue: website is not null && dealer.Website == website),
                ImportParsing.SourceField("email", record.Email, email, "Email", isCurrentValue: email is not null && dealer.Email == email),
                ImportParsing.SourceField("inventory_count", record.InventoryCount, inventoryCount, "StockCount",
                    isCurrentValue: inventoryCount.HasValue && dealer.StockCount == inventoryCount),
                ImportParsing.SourceField("last_seen", record.LastSeen),
                ImportParsing.SourceField("address", rawAddress, tradingAddress, "TradingAddress",
                    isCurrentValue: tradingAddress is not null && dealer.TradingAddress == tradingAddress));

            session.AddNameObservation(dealer, sourceRecord, "Trading", "seller_name", 0, record.SellerName);
            session.AddAddressObservation(dealer, sourceRecord, "Trading", "address", 0, rawAddress, tradingAddress,
                isPrimary: tradingAddress is not null && dealer.TradingAddress == tradingAddress);
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Maps a row from the Marketcheck dealers CSV file.
    /// </summary>
    private sealed class MarketcheckDealerRecord
    {
        [Name("mc_dealer_id")]
        public string? DealerId { get; set; }

        [Name("seller_name")]
        public string? SellerName { get; set; }

        [Name("postcode")]
        public string? Postcode { get; set; }

        [Name("street")]
        public string? Street { get; set; }

        [Name("city")]
        public string? City { get; set; }

        [Name("county")]
        public string? County { get; set; }

        [Name("phone")]
        public string? Phone { get; set; }

        [Name("website")]
        public string? Website { get; set; }

        [Name("email")]
        public string? Email { get; set; }

        [Name("inventory_count")]
        public string? InventoryCount { get; set; }

        [Name("last_seen")]
        public string? LastSeen { get; set; }
    }
}
