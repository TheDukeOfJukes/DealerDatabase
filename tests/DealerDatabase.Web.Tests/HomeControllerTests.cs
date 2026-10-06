using System.Diagnostics;
using DealerDatabase.Data;
using DealerDatabase.Data.Entities;
using DealerDatabase.Web.Models;
using Microsoft.AspNetCore.Http;
using DealerDatabase.Web.Controllers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;

namespace DealerDatabase.Web.Tests;

/// <summary>
/// Verifies dealer listing and error handling behavior in the home controller.
/// </summary>
public sealed class HomeControllerTests
{
    [Test]
    public async Task WhenErrorIsRequestedThenActivityIdIsIncludedInTheModel()
    {
        var options = new DbContextOptionsBuilder<DealerDbContext>()
            .UseSqlite("Data Source=:memory:")
            .Options;
        await using var db = new DealerDbContext(options);
        var controller = new HomeController(db)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            }
        };
        using var activity = new Activity("error-request");
        activity.Start();

        var result = controller.Error();

        Assert.That(result, Is.InstanceOf<ViewResult>());
        var viewResult = (ViewResult)result;
        Assert.That(viewResult.Model, Is.InstanceOf<ErrorViewModel>());
        Assert.That(((ErrorViewModel)viewResult.Model!).RequestId, Is.EqualTo(activity.Id));
    }

    [Test]
    public async Task WhenIndexIsRequestedThenDealersAreReturnedInLegalNameOrder()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<DealerDbContext>()
            .UseSqlite(connection)
            .Options;
        await using var db = new DealerDbContext(options);
        await db.Database.EnsureCreatedAsync();
        db.Dealers.AddRange(
            new Dealer { LegalName = "Zeta Motors" },
            new Dealer { LegalName = "Alpha Motors" });
        await db.SaveChangesAsync();

        var controller = new HomeController(db);
        var result = await controller.Index(CancellationToken.None);

        Assert.That(result, Is.InstanceOf<ViewResult>());
        var viewResult = (ViewResult)result;
        var dealers = viewResult.Model as List<Dealer>;
        Assert.That(dealers, Is.Not.Null);
        Assert.That(dealers!.Select(dealer => dealer.LegalName), Is.EqualTo(["Alpha Motors", "Zeta Motors"]));
    }
}
