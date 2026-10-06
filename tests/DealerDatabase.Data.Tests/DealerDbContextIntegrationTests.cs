using DealerDatabase.Data;
using DealerDatabase.Data.Entities;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;

namespace DealerDatabase.Data.Tests;

/// <summary>
/// Verifies persistence constraints and cascade behavior against SQLite.
/// </summary>
public sealed class DealerDbContextIntegrationTests
{
    [Test]
    public async Task WhenDuplicateCompanyRegistrationNumbersAreSavedThenSqliteRejectsTheWrite()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<DealerDbContext>()
            .UseSqlite(connection)
            .Options;
        await using var db = new DealerDbContext(options);
        await db.Database.EnsureCreatedAsync();

        db.Dealers.Add(new Dealer { LegalName = "First Dealer", CompanyRegistrationNumber = "12345678" });
        await db.SaveChangesAsync();
        db.Dealers.Add(new Dealer { LegalName = "Second Dealer", CompanyRegistrationNumber = "12345678" });

        Assert.ThrowsAsync<DbUpdateException>(async () =>
        {
            await db.SaveChangesAsync();
        });
    }

    [Test]
    public async Task WhenADealerIsDeletedThenItsSourceRecordsAreDeleted()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<DealerDbContext>()
            .UseSqlite(connection)
            .Options;
        await using var db = new DealerDbContext(options);
        await db.Database.EnsureCreatedAsync();
        db.Dealers.Add(new Dealer
        {
            LegalName = "Example Dealer",
            SourceRecords =
            [
                new DealerSourceRecord
                {
                    SourceSystem = "TestSource",
                    SourceRecordId = "record-1",
                    ProcessedAt = DateTime.UtcNow
                }
            ]
        });
        await db.SaveChangesAsync();

        db.Dealers.Remove(await db.Dealers.SingleAsync());
        await db.SaveChangesAsync();

        Assert.That(await db.SourceRecords.CountAsync(), Is.Zero);
    }
}
