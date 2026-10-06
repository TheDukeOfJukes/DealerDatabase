using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;

namespace DealerDatabase.Import.Importers;

/// <summary>
/// Imports VAT lookup results for dealers matched by name and postcode.
/// </summary>
internal sealed class VatLookupImporter(DealerImportSession session, ILogger logger)
{
    internal async Task ImportAsync(string dataDir, CancellationToken cancellationToken)
    {
        var directory = Path.Combine(dataDir, "vat_lookups");
        if (!Directory.Exists(directory))
        {
            logger.LogWarning("VAT lookups directory not found: {Path}", directory);
            return;
        }

        foreach (var path in Directory.EnumerateFiles(directory, "*.json").Order(StringComparer.OrdinalIgnoreCase))
        {
            cancellationToken.ThrowIfCancellationRequested();

            var lookup = await ImportParsing.ReadJsonAsync<VatLookup>(path, cancellationToken);
            var target = lookup?.Target;
            var recordId = Path.GetFileNameWithoutExtension(path);
            var dealer = session.MatchBySoftKey(target?.Name, target?.Address?.Postcode);
            if (target is null || dealer is null)
            {
                continue;
            }

            var vatNumber = ImportParsing.EmptyToNull(target.VatNumber);
            var isValid = string.IsNullOrWhiteSpace(lookup?.Code);
            dealer.VatNumber = vatNumber ?? dealer.VatNumber;
            dealer.VatIsValid = isValid;
            var sourceRecord = session.AddSourceRecord(dealer, "VAT", recordId,
                ImportParsing.SourceField("code", lookup?.Code),
                ImportParsing.SourceField("target.name", target.Name, Normalization.NormalizeName(target.Name)),
                ImportParsing.SourceField("target.vatNumber", target.VatNumber, vatNumber, "VatNumber",
                    isCurrentValue: vatNumber is not null && dealer.VatNumber == vatNumber),
                ImportParsing.SourceField("target.address.postcode", target.Address?.Postcode,
                    Normalization.NormalizePostcode(target.Address?.Postcode)),
                ImportParsing.SourceField("VatIsValid", lookup?.Code, isValid, "VatIsValid", isCurrentValue: true));

            session.AddNameObservation(dealer, sourceRecord, "Source", "target.name", 0, target.Name);
        }
    }

    /// <summary>
    /// Represents one VAT lookup response.
    /// </summary>
    private sealed class VatLookup
    {
        [JsonPropertyName("code")]
        public string? Code { get; set; }

        [JsonPropertyName("target")]
        public VatTarget? Target { get; set; }
    }

    /// <summary>
    /// Represents the target business details in a VAT lookup response.
    /// </summary>
    private sealed class VatTarget
    {
        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("vatNumber")]
        public string? VatNumber { get; set; }

        [JsonPropertyName("address")]
        public VatAddress? Address { get; set; }
    }

    /// <summary>
    /// Represents the address details in a VAT lookup target.
    /// </summary>
    private sealed class VatAddress
    {
        [JsonPropertyName("postcode")]
        public string? Postcode { get; set; }
    }
}