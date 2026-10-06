using System.Text.Json;
using System.Text.Json.Serialization;
using DealerDatabase.Data.Entities;
using Microsoft.Extensions.Logging;

namespace DealerDatabase.Import.Importers;

/// <summary>
/// Imports FCA register records and associates them with consolidated dealers.
/// </summary>
internal sealed class FcaImporter(DealerImportSession session, ILogger logger)
{
    internal async Task ImportAsync(string dataDir, CancellationToken cancellationToken)
    {
        var path = Path.Combine(dataDir, "fca_register.json");
        if (!File.Exists(path))
        {
            logger.LogWarning("FCA input not found: {Path}", path);
            return;
        }

        var document = await ImportParsing.ReadJsonAsync<FcaDocument>(path, cancellationToken);
        foreach (var record in (document?.Data ?? []).OrderBy(
                     item => ImportParsing.ReadJsonString(item.Frn), StringComparer.OrdinalIgnoreCase))
        {
            cancellationToken.ThrowIfCancellationRequested();

            var frn = ImportParsing.ReadJsonString(record.Frn);
            var organisationName = ImportParsing.EmptyToNull(record.OrganisationName);
            if (frn is null || organisationName is null)
            {
                continue;
            }

            var crn = Normalization.NormalizeCrn(record.CompanyRegistrationNumber);
            Dealer dealer;
            if (crn is not null)
            {
                dealer = session.GetOrCreateByCrn(crn, "FCA", frn, organisationName);
            }
            else if ((dealer = session.FindByFrn(frn)!) is null)
            {
                dealer = session.CreateDealer(organisationName);
                session.AddSourceRecord(dealer, "FCA", frn);
            }

            dealer.FcaReferenceNumber = frn;
            dealer.FcaStatus = ImportParsing.EmptyToNull(record.Status);
            session.RegisterFrn(dealer);

            // Use the first supplied trading name as the representative; otherwise use the organisation name only when it differs from the legal name.
            var firstTradingNameIndex = record.TradingNames?.FindIndex(name => !string.IsNullOrWhiteSpace(name)) ?? -1;
            var firstTradingName = firstTradingNameIndex >= 0
                ? ImportParsing.EmptyToNull(record.TradingNames![firstTradingNameIndex])
                : null;
            var tradingName = firstTradingName ??
                (!string.Equals(organisationName, dealer.LegalName, StringComparison.OrdinalIgnoreCase)
                    ? organisationName
                    : null);
            dealer.TradingName = tradingName ?? dealer.TradingName;

            var observations = new List<FieldObservation?>
            {
                ImportParsing.SourceField("FRN", record.Frn, frn, "FcaReferenceNumber", isCurrentValue: true),
                ImportParsing.SourceField("Organisation Name", record.OrganisationName, Normalization.NormalizeName(organisationName), "TradingName",
                    isCurrentValue: tradingName is not null && string.Equals(dealer.TradingName, organisationName, StringComparison.OrdinalIgnoreCase)),
                ImportParsing.SourceField("Companies House Number", record.CompanyRegistrationNumber, crn, "CompanyRegistrationNumber",
                    isCurrentValue: crn is not null && string.Equals(dealer.CompanyRegistrationNumber, crn, StringComparison.OrdinalIgnoreCase)),
                ImportParsing.SourceField("Status", record.Status, dealer.FcaStatus, "FcaStatus", isCurrentValue: dealer.FcaStatus is not null)
            };

            if (record.TradingNames is not null)
            {
                for (var index = 0; index < record.TradingNames.Count; index++)
                {
                    var rawName = record.TradingNames[index];
                    observations.Add(ImportParsing.SourceField(
                        "Trading Names",
                        rawName,
                        Normalization.NormalizeName(rawName),
                        "TradingName",
                        index,
                        isCurrentValue: index == firstTradingNameIndex && firstTradingName is not null));
                }
            }

            var sourceRecord = session.AddSourceRecord(dealer, "FCA", frn, observations.ToArray());
            if (record.TradingNames is not null)
            {
                for (var index = 0; index < record.TradingNames.Count; index++)
                {
                    session.AddNameObservation(
                        dealer,
                        sourceRecord,
                        "Trading",
                        "Trading Names",
                        index,
                        record.TradingNames[index],
                        isPrimary: index == firstTradingNameIndex && firstTradingName is not null);
                }
            }

            if (firstTradingName is null && tradingName is not null)
            {
                session.AddNameObservation(dealer, sourceRecord, "Trading", "Organisation Name", 0, record.OrganisationName, isPrimary: true);
            }
        }
    }

    /// <summary>
    /// Represents the top-level FCA register JSON document.
    /// </summary>
    private sealed class FcaDocument
    {
        [JsonPropertyName("Data")]
        public List<FcaRecord>? Data { get; set; }
    }

    /// <summary>
    /// Represents one FCA organization record.
    /// </summary>
    private sealed class FcaRecord
    {
        [JsonPropertyName("FRN")]
        public JsonElement Frn { get; set; }

        [JsonPropertyName("Organisation Name")]
        public string? OrganisationName { get; set; }

        [JsonPropertyName("Companies House Number")]
        public string? CompanyRegistrationNumber { get; set; }

        [JsonPropertyName("Trading Names")]
        public List<string>? TradingNames { get; set; }

        [JsonPropertyName("Status")]
        public string? Status { get; set; }
    }
}