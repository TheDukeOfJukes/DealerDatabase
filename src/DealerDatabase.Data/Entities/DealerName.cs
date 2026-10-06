namespace DealerDatabase.Data.Entities;

/// <summary>
/// A name value retained with its originating source and occurrence.
/// </summary>
public class DealerName
{
    public int Id { get; set; }

    public int DealerId { get; set; }

    public Dealer Dealer { get; set; } = null!;

    public int SourceRecordId { get; set; }

    public DealerSourceRecord SourceRecord { get; set; } = null!;

    public string NameType { get; set; } = string.Empty;

    public string SourceField { get; set; } = string.Empty;

    public int Occurrence { get; set; }

    public string RawValue { get; set; } = string.Empty;

    public string? NormalizedValue { get; set; }

    public bool IsPrimary { get; set; }
}
