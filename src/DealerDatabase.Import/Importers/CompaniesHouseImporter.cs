using System.Text.Json;
using System.Text.Json.Serialization;
using DealerDatabase.Data.Entities;
using Microsoft.Extensions.Logging;

namespace DealerDatabase.Import.Importers;

/// <summary>
/// Imports Companies House registration details into the consolidated dealer records.
/// </summary>
internal sealed class CompaniesHouseImporter(DealerImportSession session, ILogger logger)
{
    internal async Task ImportAsync(string dataDir, CancellationToken cancellationToken)
    {
        var path = Path.Combine(dataDir, "companies_house.json");
        if (!File.Exists(path))
        {
            logger.LogWarning("Companies House input not found: {Path}", path);
            return;
        }

        var document = await ImportParsing.ReadJsonAsync<CompaniesHouseDocument>(path, cancellationToken);
        foreach (var record in (document?.Items ?? []).OrderBy(item => item.CompanyNumber, StringComparer.OrdinalIgnoreCase))
        {
            cancellationToken.ThrowIfCancellationRequested();

            var crn = Normalization.NormalizeCrn(record.CompanyNumber);
            if (crn is null || string.IsNullOrWhiteSpace(record.CompanyName))
            {
                continue;
            }

            var dealer = session.GetOrCreateByCrn(crn, "CompaniesHouse", crn, record.CompanyName);
            var incorporationDate = ImportParsing.ParseDate(record.DateOfCreation);
            var companyStatus = ImportParsing.EmptyToNull(record.CompanyStatus);
            var registeredAddress = ImportParsing.FormatAddress(
                record.RegisteredOfficeAddress?.AddressLine1,
                record.RegisteredOfficeAddress?.AddressLine2,
                record.RegisteredOfficeAddress?.Locality,
                record.RegisteredOfficeAddress?.Region,
                record.RegisteredOfficeAddress?.PostalCode,
                record.RegisteredOfficeAddress?.Country);
            var postcode = Normalization.NormalizePostcode(record.RegisteredOfficeAddress?.PostalCode);

            dealer.LegalName = record.CompanyName.Trim();
            dealer.IncorporationDate = incorporationDate;
            dealer.CompanyStatus = companyStatus;
            dealer.RegisteredAddress = registeredAddress;
            dealer.Postcode = postcode;
            var sourceRecord = session.AddSourceRecord(dealer, "CompaniesHouse", crn,
                ImportParsing.SourceField("company_number", record.CompanyNumber, crn, "CompanyRegistrationNumber", isCurrentValue: true),
                ImportParsing.SourceField("company_name", record.CompanyName, Normalization.NormalizeName(record.CompanyName), "LegalName", isCurrentValue: true),
                ImportParsing.SourceField("date_of_creation", record.DateOfCreation, incorporationDate, "IncorporationDate", isCurrentValue: incorporationDate.HasValue),
                ImportParsing.SourceField("company_status", record.CompanyStatus, companyStatus, "CompanyStatus", isCurrentValue: companyStatus is not null),
                ImportParsing.SourceField("registered_office_address", record.RegisteredOfficeAddress, registeredAddress, "RegisteredAddress", isCurrentValue: registeredAddress is not null),
                ImportParsing.SourceField("postal_code", record.RegisteredOfficeAddress?.PostalCode, postcode, "Postcode", isCurrentValue: postcode is not null));

            session.AddNameObservation(dealer, sourceRecord, "Legal", "company_name", 0, record.CompanyName, isPrimary: true);
            session.AddAddressObservation(
                dealer,
                sourceRecord,
                "Registered",
                "registered_office_address",
                0,
                record.RegisteredOfficeAddress is null ? null : JsonSerializer.Serialize(record.RegisteredOfficeAddress),
                registeredAddress,
                isPrimary: registeredAddress is not null);
        }
    }

    /// <summary>
    /// Represents the top-level Companies House JSON document.
    /// </summary>
    private sealed class CompaniesHouseDocument
    {
        [JsonPropertyName("items")]
        public List<CompaniesHouseRecord>? Items { get; set; }
    }

    /// <summary>
    /// Represents one company entry in a Companies House document.
    /// </summary>
    private sealed class CompaniesHouseRecord
    {
        [JsonPropertyName("company_number")]
        public string? CompanyNumber { get; set; }

        [JsonPropertyName("company_name")]
        public string? CompanyName { get; set; }

        [JsonPropertyName("company_status")]
        public string? CompanyStatus { get; set; }

        [JsonPropertyName("date_of_creation")]
        public string? DateOfCreation { get; set; }

        [JsonPropertyName("registered_office_address")]
        public RegisteredOfficeAddress? RegisteredOfficeAddress { get; set; }
    }

    /// <summary>
    /// Represents the registered-office address supplied for a company.
    /// </summary>
    private sealed class RegisteredOfficeAddress
    {
        [JsonPropertyName("address_line_1")]
        public string? AddressLine1 { get; set; }

        [JsonPropertyName("address_line_2")]
        public string? AddressLine2 { get; set; }

        [JsonPropertyName("locality")]
        public string? Locality { get; set; }

        [JsonPropertyName("region")]
        public string? Region { get; set; }

        [JsonPropertyName("postal_code")]
        public string? PostalCode { get; set; }

        [JsonPropertyName("country")]
        public string? Country { get; set; }
    }
}