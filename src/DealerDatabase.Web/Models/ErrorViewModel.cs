namespace DealerDatabase.Web.Models;

/// <summary>
/// Supplies request identification details to the error view.
/// </summary>
public class ErrorViewModel
{
    /// <summary>Gets or sets the identifier for the request that failed.</summary>
    public string? RequestId { get; set; }

    /// <summary>Gets whether the request identifier should be displayed.</summary>
    public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);
}
