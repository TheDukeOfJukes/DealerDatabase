using DealerDatabase.Data;
using DealerDatabase.Import.Importers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using NUnit.Framework;

namespace DealerDatabase.Import.Tests;

/// <summary>
/// Verifies validation of the import data directory.
/// </summary>
public sealed class MatchingEngineValidationTests
{
    [Test]
    public async Task WhenDataDirectoryIsEmptyThenArgumentExceptionIsThrown()
    {
        var options = new DbContextOptionsBuilder<DealerDbContext>()
            .UseSqlite("Data Source=:memory:")
            .Options;
        await using var db = new DealerDbContext(options);
        var orchestrator = new DealerImportOrchestrator(db, NullLogger<DealerImportOrchestrator>.Instance);

        Assert.ThrowsAsync<ArgumentException>(async () => await orchestrator.RunImportAsync(string.Empty));
    }

    [Test]
    public async Task WhenDataDirectoryDoesNotExistThenDirectoryNotFoundExceptionIsThrown()
    {
        var options = new DbContextOptionsBuilder<DealerDbContext>()
            .UseSqlite("Data Source=:memory:")
            .Options;
        await using var db = new DealerDbContext(options);
        var orchestrator = new DealerImportOrchestrator(db, NullLogger<DealerImportOrchestrator>.Instance);
        var missingDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));

        Assert.ThrowsAsync<DirectoryNotFoundException>(async () => await orchestrator.RunImportAsync(missingDirectory));
    }
}
