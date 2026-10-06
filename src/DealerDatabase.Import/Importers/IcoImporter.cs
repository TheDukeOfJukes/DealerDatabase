using CsvHelper.Configuration.Attributes;
using Microsoft.Extensions.Logging;

namespace DealerDatabase.Import.Importers;

/// <summary>
/// Imports ICO registration details for dealers matched by company registration number.
/// </summary>
internal sealed class IcoImporter(DealerImportSession session, ILogger logger)
{
    internal Task ImportAsync(string dataDir, CancellationToken cancellationToken)
    {
        var path = Path.Combine(dataDir, "ico_register.csv");
        if (!File.Exists(path))
        {
            logger.LogWarning("ICO input not found: {Path}", path);
            return Task.CompletedTask;
        }

        foreach (var record in ImportParsing.ReadCsv<IcoRecord>(path))
        {
            cancellationToken.ThrowIfCancellationRequested();

            var registrationNumber = ImportParsing.EmptyToNull(record.RegistrationNumber);
            var dealer = session.FindByCrn(record.CompanyRegistrationNumber);
            if (registrationNumber is null || dealer is null)
            {
                continue;
            }

            var expiry = ImportParsing.ParseDate(record.EndDateOfRegistration);
            var organizationName = ImportParsing.EmptyToNull(record.OrganisationName);
            var tradingNames = ImportParsing.EmptyToNull(record.TradingNames);
            var normalizedCrn = Normalization.NormalizeCrn(record.CompanyRegistrationNumber);
            var normalizedPostcode = Normalization.NormalizePostcode(record.OrganisationPostcode);
            var rawAddress = ImportParsing.FormatRawAddress(
                record.OrganisationAddressLine1,
                record.OrganisationAddressLine2,
                record.OrganisationAddressLine3,
                record.OrganisationPostcode);
            var normalizedAddress = ImportParsing.FormatAddress(
                record.OrganisationAddressLine1,
                record.OrganisationAddressLine2,
                record.OrganisationAddressLine3,
                record.OrganisationPostcode);
            dealer.IcoRegistrationNumber = registrationNumber;
            dealer.IcoExpiry = expiry;
            var sourceRecord = session.AddSourceRecord(dealer, "ICO", registrationNumber,
                ImportParsing.SourceField("Registration_number", record.RegistrationNumber, registrationNumber, "IcoRegistrationNumber", isCurrentValue: true),
                ImportParsing.SourceField("Company_registration_number", record.CompanyRegistrationNumber, normalizedCrn, "CompanyRegistrationNumber",
                    isCurrentValue: string.Equals(dealer.CompanyRegistrationNumber, normalizedCrn, StringComparison.OrdinalIgnoreCase)),
                ImportParsing.SourceField("Organisation_name", record.OrganisationName, Normalization.NormalizeName(organizationName)),
                ImportParsing.SourceField("Trading_names", record.TradingNames, Normalization.NormalizeName(tradingNames)),
                ImportParsing.SourceField("Organisation_address_line_1", record.OrganisationAddressLine1),
                ImportParsing.SourceField("Organisation_address_line_2", record.OrganisationAddressLine2),
                ImportParsing.SourceField("Organisation_address_line_3", record.OrganisationAddressLine3),
                ImportParsing.SourceField("Organisation_postcode", record.OrganisationPostcode, normalizedPostcode),
                ImportParsing.SourceField("Organisation_address", rawAddress, normalizedAddress),
                ImportParsing.SourceField("End_date_of_registration", record.EndDateOfRegistration, expiry, "IcoExpiry", isCurrentValue: expiry.HasValue));
            session.AddNameObservation(dealer, sourceRecord, "Source", "Organisation_name", 0, record.OrganisationName);
            session.AddNameObservation(dealer, sourceRecord, "Trading", "Trading_names", 0, record.TradingNames);
            session.AddAddressObservation(dealer, sourceRecord, "Registered", "Organisation_address", 0, rawAddress, normalizedAddress);
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Maps a row from the ICO register CSV file.
    /// </summary>
    private sealed class IcoRecord
    {
        [Name("Registration_number")]
        public string? RegistrationNumber { get; set; }

        [Name("Company_registration_number")]
        public string? CompanyRegistrationNumber { get; set; }

        [Name("Organisation_name")]
        public string? OrganisationName { get; set; }

        [Name("Trading_names")]
        public string? TradingNames { get; set; }

        [Name("Organisation_address_line_1")]
        public string? OrganisationAddressLine1 { get; set; }

        [Name("Organisation_address_line_2")]
        public string? OrganisationAddressLine2 { get; set; }

        [Name("Organisation_address_line_3")]
        public string? OrganisationAddressLine3 { get; set; }

        [Name("Organisation_postcode")]
        public string? OrganisationPostcode { get; set; }

        [Name("End_date_of_registration")]
        public string? EndDateOfRegistration { get; set; }
    }
}