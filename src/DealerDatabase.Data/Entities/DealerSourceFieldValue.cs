namespace DealerDatabase.Data.Entities;

/// <summary>
/// A raw and normalized field value reported by a source record.
/// </summary>
public class DealerSourceFieldValue
{
    public int Id { get; set; }

    public int SourceRecordId { get; set; }

    public DealerSourceRecord SourceRecord { get; set; } = null!;

    public string FieldName { get; set; } = string.Empty;

    public string? ConsolidatedFieldName { get; set; }

    public int Occurrence { get; set; }

    public string RawValue { get; set; } = string.Empty;

    public string? NormalizedValue { get; set; }

    public bool IsCurrentValue { get; set; }
}
