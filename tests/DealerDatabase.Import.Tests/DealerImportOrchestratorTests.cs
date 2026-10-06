using System.Text.Json;
using DealerDatabase.Data;
using DealerDatabase.Import.Importers;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using NUnit.Framework;

namespace DealerDatabase.Import.Tests;

/// <summary>
/// Verifies source matching, data consolidation, and repeat-import stability.
/// </summary>
public sealed class MatchingEngineTests
{
    [Test]
    public async Task WhenSourcesMatchThenDataIsConsolidatedAndContributionsAreRecorded()
    {
        var dataDirectory = CreateSampleDataDirectory();

        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<DealerDbContext>()
            .UseSqlite(connection)
            .Options;
        await using var db = new DealerDbContext(options);
        await db.Database.EnsureCreatedAsync();

        var orchestrator = new DealerImportOrchestrator(db, NullLogger<DealerImportOrchestrator>.Instance);
        await orchestrator.RunImportAsync(dataDirectory);

        var dealers = await db.Dealers.Include(dealer => dealer.SourceRecords).ToListAsync();
        Assert.That(dealers, Has.Count.EqualTo(2));

        var company = dealers.Single(dealer => dealer.CompanyRegistrationNumber == "12345678");
        Assert.That(company.Telephone, Is.EqualTo("+441134960002"));
        Assert.That(company.Website, Is.EqualTo("https://example.co.uk/used-cars"));
        Assert.That(company.TradingAddress, Is.EqualTo("York, YO11AA"));
        Assert.That(company.StockCount, Is.EqualTo(42));
        Assert.That(company.VatNumber, Is.EqualTo("GB123456789"));
        Assert.That(company.VatIsValid, Is.True);
        Assert.That(company.IcoRegistrationNumber, Is.EqualTo("ZA123456"));

        var companiesHouseRecord = company.SourceRecords.Single(source => source.SourceSystem == "CompaniesHouse");
        using var fields = JsonDocument.Parse(companiesHouseRecord.ContributedFieldsJson);
        Assert.That(fields.RootElement.EnumerateArray().Select(value => value.GetString()), Does.Contain("company_name"));

        var fcaDealer = dealers.Single(dealer => dealer.FcaReferenceNumber == "111111");
        Assert.That(fcaDealer.CompanyRegistrationNumber, Is.EqualTo("87654321"));
        Assert.That(fcaDealer.SourceRecords.Any(source => source.SourceSystem == "Crawler"), Is.True);
    }

    [Test]
    public async Task WhenTheSameImportRunsTwiceThenDealersAndLineageAreNotDuplicated()
    {
        var dataDirectory = CreateSampleDataDirectory();

        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<DealerDbContext>()
            .UseSqlite(connection)
            .Options;
        await using var db = new DealerDbContext(options);
        await db.Database.EnsureCreatedAsync();

        var orchestrator = new DealerImportOrchestrator(db, NullLogger<DealerImportOrchestrator>.Instance);
        await orchestrator.RunImportAsync(dataDirectory);
        var dealerCount = await db.Dealers.CountAsync();
        var sourceRecordCount = await db.SourceRecords.CountAsync();

        await orchestrator.RunImportAsync(dataDirectory);

        Assert.That(await db.Dealers.CountAsync(), Is.EqualTo(dealerCount));
        Assert.That(await db.SourceRecords.CountAsync(), Is.EqualTo(sourceRecordCount));
    }

    [Test]
    public async Task WhenTheFullSampleImportRunsTwiceThenDealerAndLineageCountsRemainStable()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<DealerDbContext>()
            .UseSqlite(connection)
            .Options;
        await using var db = new DealerDbContext(options);
        await db.Database.EnsureCreatedAsync();

        var orchestrator = new DealerImportOrchestrator(db, NullLogger<DealerImportOrchestrator>.Instance);
        await orchestrator.RunImportAsync(SolutionPaths.DataDirectory);
        var dealerCount = await db.Dealers.CountAsync();
        var sourceRecordCount = await db.SourceRecords.CountAsync();

        await orchestrator.RunImportAsync(SolutionPaths.DataDirectory);

        Assert.That(dealerCount, Is.GreaterThan(0));
        Assert.That(sourceRecordCount, Is.GreaterThan(0));
        Assert.That(await db.Dealers.CountAsync(), Is.EqualTo(dealerCount));
        Assert.That(await db.SourceRecords.CountAsync(), Is.EqualTo(sourceRecordCount));
    }

    private static string CreateSampleDataDirectory()
    {
        var directory = Path.Combine(Path.GetTempPath(), $"dealer-import-tests-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);

        File.WriteAllText(Path.Combine(directory, "companies_house.json"), """
            {"items":[{"company_number":"12345678.0","company_name":"Example Motors Limited","company_status":"active","date_of_creation":"2001-02-03","registered_office_address":{"address_line_1":"1 Main Street","locality":"York","postal_code":"YO1 1AA","country":"England"}}]}
            """);

        File.WriteAllText(Path.Combine(directory, "fca_register.json"), """
            {"Data":[{"FRN":111111,"Organisation Name":"Independent Dealer Limited","Status":"Authorised","Companies House Number":null,"Trading Names":[]}]}
            """);

        WriteCsv(Path.Combine(directory, "ico_register.csv"),
            ["Registration_number", "Company_registration_number", "Organisation_name", "Trading_names", "Organisation_address_line_1", "Organisation_address_line_2", "Organisation_address_line_3", "Organisation_postcode", "End_date_of_registration"],
            new Dictionary<string, string>
            {
                ["Registration_number"] = "ZA123456",
                ["Company_registration_number"] = "12345678.0",
                ["Organisation_name"] = "Example Motors Limited",
                ["Trading_names"] = "Example Autos",
                ["Organisation_address_line_1"] = "1 Main Street",
                ["Organisation_address_line_2"] = "York",
                ["Organisation_address_line_3"] = "North Yorkshire",
                ["Organisation_postcode"] = "YO1 1AA",
                ["End_date_of_registration"] = "30/05/2027"
            });

        File.WriteAllText(Path.Combine(directory, "saf_members.xml"), """
            <SafRegister><Member id="SAF-1"><Name>Example Motors Limited</Name><Town>York</Town><Postcode>YO11AA</Postcode><Telephone>0113 496 0001</Telephone><Website>www.Example.co.uk/</Website><Status>Active</Status><Expiry>2027-01-31</Expiry></Member></SafRegister>
            """);

        WriteCsv(Path.Combine(directory, "crawled_dealers.csv"),
            ["crawl_id", "crawled_at", "source_url", "final_url", "http_status", "business_name_detected", "address_detected", "postcode_detected", "phones_detected", "emails_detected", "company_number_detected", "vat_number_detected", "fca_frn_detected", "stock_count_detected"],
            new Dictionary<string, string>
            {
                ["crawl_id"] = "CRW-1",
                ["crawled_at"] = "2026-09-19T17:23:00Z",
                ["source_url"] = "crawler.example.test",
                ["http_status"] = "200",
                ["business_name_detected"] = "Unrelated crawled name",
                ["company_number_detected"] = "87654321.0",
                ["fca_frn_detected"] = "111111"
            });

        Directory.CreateDirectory(Path.Combine(directory, "vat_lookups"));
        File.WriteAllText(Path.Combine(directory, "vat_lookups", "GB123456789.json"), """
            {"target":{"name":"Example Motors Limited","vatNumber":"GB123456789","address":{"postcode":"YO1 1AA"}}}
            """);

        WriteCsv(Path.Combine(directory, "marketcheck_dealers.csv"),
            ["mc_dealer_id", "seller_name", "postcode", "street", "city", "county", "phone", "website", "email", "inventory_count", "last_seen"],
            new Dictionary<string, string>
            {
                ["mc_dealer_id"] = "MC-1",
                ["seller_name"] = "EXAMPLE MOTORS LTD",
                ["postcode"] = "YO11AA",
                ["street"] = "Unit 1",
                ["city"] = "York",
                ["county"] = "North Yorkshire",
                ["phone"] = "+44 (0)113 496 0002",
                ["website"] = "example.co.uk/used-cars",
                ["email"] = "sales@example.co.uk",
                ["inventory_count"] = "42",
                ["last_seen"] = "2026-09-26"
            });

        return directory;
    }

    private static void WriteCsv(string path, IReadOnlyList<string> headers, IReadOnlyDictionary<string, string> values)
    {
        static string Escape(string value) => value.Contains(',') || value.Contains('"')
            ? $"\"{value.Replace("\"", "\"\"")}\""
            : value;

        var headerLine = string.Join(',', headers);
        var row = string.Join(',', headers.Select(header => Escape(values.GetValueOrDefault(header, string.Empty))));
        File.WriteAllText(path, $"{headerLine}{Environment.NewLine}{row}{Environment.NewLine}");
    }
}
