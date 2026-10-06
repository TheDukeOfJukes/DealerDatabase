namespace DealerDatabase.Data.Entities;

/// <summary>
/// A dealership record consolidated from registry and enrichment sources.
/// </summary>
public class Dealer
{
    public int Id { get; set; }

    public string LegalName { get; set; } = string.Empty;

    public string? TradingName { get; set; }

    public string? CompanyRegistrationNumber { get; set; }

    public DateTime? IncorporationDate { get; set; }

    public string? CompanyStatus { get; set; }

    public string? RegisteredAddress { get; set; }

    public string? TradingAddress { get; set; }

    public string? Postcode { get; set; }

    public string? Telephone { get; set; }

    public string? Email { get; set; }

    public string? Website { get; set; }

    public string? FcaReferenceNumber { get; set; }

    public string? FcaStatus { get; set; }

    public string? IcoRegistrationNumber { get; set; }

    public DateTime? IcoExpiry { get; set; }

    public string? SafStatus { get; set; }

    public DateTime? SafExpiry { get; set; }

    public string? VatNumber { get; set; }

    public bool? VatIsValid { get; set; }

    public int? StockCount { get; set; }

    public ICollection<DealerSourceRecord> SourceRecords { get; set; } = new List<DealerSourceRecord>();
    public ICollection<DealerName> Names { get; set; } = new List<DealerName>();

    public ICollection<DealerAddress> Addresses { get; set; } = new List<DealerAddress>();
}
