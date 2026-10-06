using System.Diagnostics;
using DealerDatabase.Data;
using DealerDatabase.Web.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DealerDatabase.Web.Controllers;

/// <summary>
/// Handles the dealer list and application error views.
/// </summary>
public class HomeController(DealerDbContext db) : Controller
{
    /// <summary>Displays dealers ordered by legal name.</summary>
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var dealers = await db.Dealers
            .AsNoTracking()
            .OrderBy(d => d.LegalName)
            .ToListAsync(cancellationToken);

        return View(dealers);
    }

    /// <summary>Displays the error page with the current request identifier.</summary>
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
