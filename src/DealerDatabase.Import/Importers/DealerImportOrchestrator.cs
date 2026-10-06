using DealerDatabase.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DealerDatabase.Import.Importers;

/// <summary>
/// Coordinates source imports and persists the consolidated dealer records.
/// </summary>
public sealed class DealerImportOrchestrator(DealerDbContext db, ILogger<DealerImportOrchestrator> logger)
{
    /// <summary>
    /// Runs the source import passes in precedence order and persists the consolidated dealers.
    /// </summary>
    public async Task RunImportAsync(string dataDir, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(dataDir);

        if (!Directory.Exists(dataDir))
        {
            throw new DirectoryNotFoundException($"Data directory '{dataDir}' does not exist.");
        }

        var dealers = await db.Dealers
            .Include(dealer => dealer.SourceRecords)
                .ThenInclude(sourceRecord => sourceRecord.FieldValues)
            .Include(dealer => dealer.SourceRecords)
                .ThenInclude(sourceRecord => sourceRecord.Names)
            .Include(dealer => dealer.SourceRecords)
                .ThenInclude(sourceRecord => sourceRecord.Addresses)
            .Include(dealer => dealer.Names)
            .Include(dealer => dealer.Addresses)
            .AsSplitQuery()
            .OrderBy(dealer => dealer.Id)
            .ToListAsync(cancellationToken);
        var session = new DealerImportSession(db, dealers);

        // Identity sources run first; later enrichment passes can then match and update their consolidated records.
        await new CompaniesHouseImporter(session, logger).ImportAsync(dataDir, cancellationToken);
        await new FcaImporter(session, logger).ImportAsync(dataDir, cancellationToken);
        await new IcoImporter(session, logger).ImportAsync(dataDir, cancellationToken);
        await new SafImporter(session, logger).ImportAsync(dataDir, cancellationToken);
        await new CrawledDealersImporter(session, logger).ImportAsync(dataDir, cancellationToken);
        await new VatLookupImporter(session, logger).ImportAsync(dataDir, cancellationToken);
        await new MarketcheckImporter(session, logger).ImportAsync(dataDir, cancellationToken);

        await db.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Import completed with {DealerCount} consolidated dealers.", session.DealerCount);
    }
}