namespace DealerDatabase.Data.Entities;

/// <summary>
/// Identifies one source record that contributed data to a dealer.
/// </summary>
public class DealerSourceRecord
{
    public int Id { get; set; }

    public int DealerId { get; set; }

    public Dealer Dealer { get; set; } = null!;

    public string SourceSystem { get; set; } = string.Empty;

    public string SourceRecordId { get; set; } = string.Empty;

    public string ContributedFieldsJson { get; set; } = "[]";

    public DateTime ProcessedAt { get; set; }

    public ICollection<DealerName> Names { get; set; } = new List<DealerName>();

    public ICollection<DealerAddress> Addresses { get; set; } = new List<DealerAddress>();

    public ICollection<DealerSourceFieldValue> FieldValues { get; set; } = new List<DealerSourceFieldValue>();
}
