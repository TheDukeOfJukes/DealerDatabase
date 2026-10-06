using DealerDatabase.Data;
using DealerDatabase.Import.Importers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddDealerDatabase();
builder.Services.AddTransient<DealerImportOrchestrator>();

using var host = builder.Build();

var logger = host.Services.GetRequiredService<ILogger<Program>>();

await using (var scope = host.Services.CreateAsyncScope())
{
    var db = scope.ServiceProvider.GetRequiredService<DealerDbContext>();
    await db.Database.MigrateAsync();
    var orchestrator = scope.ServiceProvider.GetRequiredService<DealerImportOrchestrator>();
    await orchestrator.RunImportAsync(SolutionPaths.DataDirectory);
}

logger.LogInformation("Database: {DatabaseFile}", SolutionPaths.DatabaseFile);
logger.LogInformation("Data folder: {DataDirectory}", SolutionPaths.DataDirectory);

foreach (var entry in Directory.EnumerateFileSystemEntries(SolutionPaths.DataDirectory).Order())
{
    logger.LogInformation("  Found source: {Name}", Path.GetFileName(entry));
}

