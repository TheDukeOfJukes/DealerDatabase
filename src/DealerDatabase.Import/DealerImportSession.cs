using System.Text.Json;
using DealerDatabase.Data;
using DealerDatabase.Data.Entities;

namespace DealerDatabase.Import;

/// <summary>
/// Maintains in-memory dealer indexes and source observations during an import run.
/// </summary>
internal sealed class DealerImportSession
{
    private readonly DealerDbContext _db;
    private readonly List<Dealer> _dealers = [];
    private readonly Dictionary<string, Dealer> _dealersByCrn = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, Dealer> _dealersByFrn = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, Dealer> _dealersBySourceKey = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, DealerSourceRecord> _sourceRecordsByKey = new(StringComparer.OrdinalIgnoreCase);

    internal DealerImportSession(DealerDbContext db, IEnumerable<Dealer> dealers)
    {
        _db = db;
        _dealers.AddRange(dealers);

        foreach (var dealer in _dealers)
        {
            RegisterCrn(dealer);
            RegisterFrn(dealer);

            foreach (var sourceRecord in dealer.SourceRecords)
            {
                var sourceKey = ImportParsing.CreateSourceKey(sourceRecord.SourceSystem, sourceRecord.SourceRecordId);
                _sourceRecordsByKey.TryAdd(sourceKey, sourceRecord);
                _dealersBySourceKey.TryAdd(sourceKey, dealer);
            }
        }
    }

    internal int DealerCount => _dealers.Count;

    internal Dealer? FindByCrn(string? crn)
    {
        var normalizedCrn = Normalization.NormalizeCrn(crn);
        return normalizedCrn is not null && _dealersByCrn.TryGetValue(normalizedCrn, out var dealer)
            ? dealer
            : null;
    }

    internal Dealer? FindByFrn(string? frn)
    {
        var normalizedFrn = ImportParsing.EmptyToNull(frn);
        return normalizedFrn is not null && _dealersByFrn.TryGetValue(normalizedFrn, out var dealer)
            ? dealer
            : null;
    }

    internal Dealer? FindBySourceKey(string system, string recordId)
    {
        var sourceKey = ImportParsing.CreateSourceKey(system, recordId);
        return _dealersBySourceKey.TryGetValue(sourceKey, out var dealer) ? dealer : null;
    }

    internal Dealer? MatchBySoftKey(string? name, string? postcode)
    {
        var normalizedName = Normalization.NormalizeName(name);
        var normalizedPostcode = Normalization.NormalizePostcode(postcode);
        if (normalizedName is null || normalizedPostcode is null)
        {
            return null;
        }

        // Requiring both normalized fields keeps a partial or name-only match from merging unrelated dealers.
        return _dealers.FirstOrDefault(dealer =>
            Normalization.NormalizeName(dealer.LegalName) == normalizedName &&
            Normalization.NormalizePostcode(dealer.Postcode) == normalizedPostcode);
    }

    internal Dealer GetOrCreateByCrn(string crn, string source, string sourceId, string legalName)
    {
        var normalizedCrn = Normalization.NormalizeCrn(crn)
            ?? throw new ArgumentException("A valid company registration number is required.", nameof(crn));

        if (_dealersByCrn.TryGetValue(normalizedCrn, out var existing))
        {
            AddSourceRecord(existing, source, sourceId);
            return existing;
        }

        var dealer = CreateDealer(legalName);
        dealer.CompanyRegistrationNumber = normalizedCrn;
        RegisterCrn(dealer);
        AddSourceRecord(dealer, source, sourceId);
        return dealer;
    }

    internal Dealer CreateDealer(string legalName)
    {
        var dealer = new Dealer { LegalName = legalName.Trim() };
        _dealers.Add(dealer);
        _db.Dealers.Add(dealer);
        return dealer;
    }

    internal void RegisterCrn(Dealer dealer)
    {
        var crn = Normalization.NormalizeCrn(dealer.CompanyRegistrationNumber);
        if (crn is not null)
        {
            _dealersByCrn.TryAdd(crn, dealer);
        }
    }

    internal void RegisterFrn(Dealer dealer)
    {
        var frn = ImportParsing.EmptyToNull(dealer.FcaReferenceNumber);
        if (frn is not null)
        {
            _dealersByFrn.TryAdd(frn, dealer);
        }
    }

    internal DealerSourceRecord AddSourceRecord(
        Dealer dealer,
        string system,
        string recordId,
        params FieldObservation?[] contributedFields)
    {
        var normalizedSystem = ImportParsing.EmptyToNull(system);
        var normalizedRecordId = ImportParsing.EmptyToNull(recordId);
        if (normalizedSystem is null || normalizedRecordId is null)
        {
            throw new ArgumentException("Source system and record ID are required.");
        }

        var sourceKey = ImportParsing.CreateSourceKey(normalizedSystem, normalizedRecordId);
        if (!_sourceRecordsByKey.TryGetValue(sourceKey, out var sourceRecord))
        {
            sourceRecord = new DealerSourceRecord
            {
                Dealer = dealer,
                DealerId = dealer.Id,
                SourceSystem = normalizedSystem,
                SourceRecordId = normalizedRecordId,
                ProcessedAt = DateTime.UtcNow
            };
            dealer.SourceRecords.Add(sourceRecord);
            _db.SourceRecords.Add(sourceRecord);
            _sourceRecordsByKey.Add(sourceKey, sourceRecord);
            _dealersBySourceKey.Add(sourceKey, dealer);
        }

        // Re-imports update the same source record while retaining the union of fields observed for it.
        sourceRecord.ContributedFieldsJson = MergeContributedFields(
            sourceRecord.ContributedFieldsJson,
            contributedFields.Where(field => field.HasValue).Select(field => field!.Value.FieldName));

        foreach (var field in contributedFields)
        {
            if (field.HasValue)
            {
                AddOrUpdateSourceFieldValue(dealer, sourceRecord, field.Value);
            }
        }

        return sourceRecord;
    }

    internal void AddNameObservation(
        Dealer dealer,
        DealerSourceRecord sourceRecord,
        string nameType,
        string sourceField,
        int occurrence,
        string? rawValue,
        bool isPrimary = false)
    {
        if (string.IsNullOrWhiteSpace(rawValue))
        {
            return;
        }

        var name = sourceRecord.Names.FirstOrDefault(value =>
            value.SourceField == sourceField && value.Occurrence == occurrence);
        if (name is null)
        {
            name = new DealerName
            {
                Dealer = dealer,
                DealerId = dealer.Id,
                SourceRecord = sourceRecord,
                SourceRecordId = sourceRecord.Id,
                SourceField = sourceField,
                Occurrence = occurrence
            };
            sourceRecord.Names.Add(name);
            dealer.Names.Add(name);
            _db.DealerNames.Add(name);
        }

        name.NameType = nameType;
        name.RawValue = rawValue;
        name.NormalizedValue = Normalization.NormalizeName(rawValue);
        if (isPrimary)
        {
            foreach (var otherName in dealer.Names.Where(value => value.NameType == nameType))
            {
                otherName.IsPrimary = false;
            }
        }

        name.IsPrimary = isPrimary;
    }

    internal void AddAddressObservation(
        Dealer dealer,
        DealerSourceRecord sourceRecord,
        string addressType,
        string sourceField,
        int occurrence,
        string? rawValue,
        string? normalizedValue,
        bool isPrimary = false)
    {
        if (string.IsNullOrWhiteSpace(rawValue))
        {
            return;
        }

        var address = sourceRecord.Addresses.FirstOrDefault(value =>
            value.SourceField == sourceField && value.Occurrence == occurrence);
        if (address is null)
        {
            address = new DealerAddress
            {
                Dealer = dealer,
                DealerId = dealer.Id,
                SourceRecord = sourceRecord,
                SourceRecordId = sourceRecord.Id,
                SourceField = sourceField,
                Occurrence = occurrence
            };
            sourceRecord.Addresses.Add(address);
            dealer.Addresses.Add(address);
            _db.DealerAddresses.Add(address);
        }

        address.AddressType = addressType;
        address.RawValue = rawValue;
        address.NormalizedValue = normalizedValue;
        if (isPrimary)
        {
            foreach (var otherAddress in dealer.Addresses.Where(value => value.AddressType == addressType))
            {
                otherAddress.IsPrimary = false;
            }
        }

        address.IsPrimary = isPrimary;
    }

    private void AddOrUpdateSourceFieldValue(Dealer dealer, DealerSourceRecord sourceRecord, FieldObservation observation)
    {
        var fieldValue = sourceRecord.FieldValues.FirstOrDefault(value =>
            value.FieldName == observation.FieldName && value.Occurrence == observation.Occurrence);
        if (fieldValue is null)
        {
            fieldValue = new DealerSourceFieldValue
            {
                SourceRecord = sourceRecord,
                SourceRecordId = sourceRecord.Id,
                FieldName = observation.FieldName,
                Occurrence = observation.Occurrence
            };
            sourceRecord.FieldValues.Add(fieldValue);
            _db.SourceFieldValues.Add(fieldValue);
        }

        fieldValue.RawValue = observation.RawValue;
        fieldValue.NormalizedValue = observation.NormalizedValue;
        fieldValue.ConsolidatedFieldName = observation.ConsolidatedFieldName;
        fieldValue.IsCurrentValue = false;

        if (observation.IsCurrentValue && observation.ConsolidatedFieldName is not null)
        {
            // A consolidated field can have only one current source observation across a dealer's lineage.
            foreach (var previousValue in dealer.SourceRecords
                         .SelectMany(record => record.FieldValues)
                         .Where(value => value.ConsolidatedFieldName == observation.ConsolidatedFieldName))
            {
                previousValue.IsCurrentValue = false;
            }

            fieldValue.IsCurrentValue = true;
        }
    }

    private static string MergeContributedFields(string currentJson, IEnumerable<string?> contributedFields)
    {
        var fields = JsonSerializer.Deserialize<HashSet<string>>(currentJson) ?? new HashSet<string>(StringComparer.Ordinal);
        foreach (var field in contributedFields)
        {
            if (!string.IsNullOrWhiteSpace(field))
            {
                fields.Add(field);
            }
        }

        // Sorting makes the serialized set stable across runs even though HashSet iteration order is unspecified.
        return JsonSerializer.Serialize(fields.Order(StringComparer.Ordinal));
    }
}

internal readonly record struct FieldObservation(
    string FieldName,
    string RawValue,
    string? NormalizedValue,
    string? ConsolidatedFieldName,
    int Occurrence,
    bool IsCurrentValue);
