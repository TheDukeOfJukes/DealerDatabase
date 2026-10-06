using DealerDatabase.Data;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;

namespace DealerDatabase.Data.Tests;

/// <summary>
/// Verifies database migrations preserve existing dealer data.
/// </summary>
public sealed class DealerDbContextMigrationTests
{
    [Test]
    public async Task WhenExpandedMigrationsRunThenTheLegacyDealerNameIsPreserved()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using (var command = connection.CreateCommand())
        {
            command.CommandText = "CREATE TABLE \"Dealers\" (\"Id\" INTEGER NOT NULL CONSTRAINT \"PK_Dealers\" PRIMARY KEY AUTOINCREMENT, \"Name\" TEXT NOT NULL); INSERT INTO \"Dealers\" (\"Name\") VALUES ('Legacy Dealer'); CREATE TABLE \"__EFMigrationsHistory\" (\"MigrationId\" TEXT NOT NULL CONSTRAINT \"PK___EFMigrationsHistory\" PRIMARY KEY, \"ProductVersion\" TEXT NOT NULL); INSERT INTO \"__EFMigrationsHistory\" (\"MigrationId\", \"ProductVersion\") VALUES ('20261001090000_InitialCreate', '8.0.11');";
            await command.ExecuteNonQueryAsync();
        }

        var options = new DbContextOptionsBuilder<DealerDbContext>()
            .UseSqlite(connection)
            .Options;
        await using var db = new DealerDbContext(options);

        await db.Database.MigrateAsync();

        Assert.That(await db.Dealers.Select(dealer => dealer.LegalName).SingleAsync(), Is.EqualTo("Legacy Dealer"));
        var appliedMigrations = await db.Database.GetAppliedMigrationsAsync();
        Assert.That(appliedMigrations, Does.Contain("20261006105959_AddExpandedDealerAndSourceRecords"));
        Assert.That(appliedMigrations, Does.Contain("20261006120223_AddDealerSourceFieldMetadata"));
    }
}
