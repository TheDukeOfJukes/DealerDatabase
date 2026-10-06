namespace DealerDatabase.Data.Entities;

/// <summary>
/// An address value retained with its originating source and occurrence.
/// </summary>
public class DealerAddress
{
    public int Id { get; set; }

    public int DealerId { get; set; }

    public Dealer Dealer { get; set; } = null!;

    public int SourceRecordId { get; set; }

    public DealerSourceRecord SourceRecord { get; set; } = null!;

    public string AddressType { get; set; } = string.Empty;

    public string SourceField { get; set; } = string.Empty;

    public int Occurrence { get; set; }

    public string RawValue { get; set; } = string.Empty;

    public string? NormalizedValue { get; set; }

    public bool IsPrimary { get; set; }
}
