using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using CsvHelper;

namespace DealerDatabase.Import;

/// <summary>
/// Reads source files and converts their values into import-ready observations.
/// </summary>
internal static class ImportParsing
{
    private static readonly Regex PostcodeInAddress = new(
        @"\b[A-Z]{1,2}\d[A-Z\d]?\s*\d[A-Z]{2}\b",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

    internal static FieldObservation? SourceField(
        string fieldName,
        object? rawValue,
        object? normalizedValue = null,
        string? consolidatedFieldName = null,
        int occurrence = 0,
        bool isCurrentValue = false)
    {
        var rawText = ToSourceText(rawValue);
        if (string.IsNullOrWhiteSpace(rawText))
        {
            return null;
        }

        return new FieldObservation(
            fieldName,
            rawText,
            ToSourceText(normalizedValue) ?? rawText,
            consolidatedFieldName,
            occurrence,
            isCurrentValue);
    }

    internal static async Task<T?> ReadJsonAsync<T>(string path, CancellationToken cancellationToken)
    {
        await using var stream = File.OpenRead(path);
        return await JsonSerializer.DeserializeAsync<T>(stream, cancellationToken: cancellationToken);
    }

    internal static List<T> ReadCsv<T>(string path)
    {
        using var reader = new StreamReader(path);
        using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);
        return csv.GetRecords<T>().ToList();
    }

    // Some register exports encode identifiers as JSON numbers and others as strings.
    internal static string? ReadJsonString(JsonElement value) => value.ValueKind switch
    {
        JsonValueKind.String => EmptyToNull(value.GetString()),
        JsonValueKind.Number => value.GetRawText(),
        _ => null
    };

    internal static string? GetElementValue(XElement parent, string localName) =>
        EmptyToNull(parent.Elements()
            .FirstOrDefault(element => string.Equals(element.Name.LocalName, localName, StringComparison.OrdinalIgnoreCase))?
            .Value);

    internal static string? GetRawElementValue(XElement parent, string localName) =>
        parent.Elements()
            .FirstOrDefault(element => string.Equals(element.Name.LocalName, localName, StringComparison.OrdinalIgnoreCase))?
            .Value;

    internal static string? FormatRawAddress(params string?[] parts)
    {
        var values = parts.Where(value => !string.IsNullOrWhiteSpace(value));
        return values.Any() ? string.Join(", ", values) : null;
    }

    internal static string? FormatAddress(params string?[] parts)
    {
        var values = parts.Select(EmptyToNull).Where(value => value is not null);
        var address = string.Join(", ", values!);
        return Normalization.NormalizeAddress(address);
    }

    internal static string? NormalizePostcodeFromAddress(string? address)
    {
        var match = address is null ? Match.Empty : PostcodeInAddress.Match(address);
        return match.Success ? Normalization.NormalizePostcode(match.Value) : null;
    }

    internal static string? FirstContactValue(string? values) =>
        values?.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();

    internal static string? EmptyToNull(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    internal static DateTime? ParseDate(string? value) =>
        DateTime.TryParse(value, CultureInfo.GetCultureInfo("en-GB"), DateTimeStyles.AllowWhiteSpaces, out var date)
            ? date.Date
            : null;

    internal static int? ParseInt(string? value) =>
        int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var number)
            ? number
            : null;

    internal static string CreateSourceKey(string system, string recordId) => $"{system}\u001f{recordId}";

    private static string? ToSourceText(object? value) => value switch
    {
        null => null,
        string text => text,
        JsonElement element when element.ValueKind == JsonValueKind.String => element.GetString(),
        JsonElement element => element.GetRawText(),
        DateTime date => date.ToString("O", CultureInfo.InvariantCulture),
        IFormattable formattable => formattable.ToString(null, CultureInfo.InvariantCulture),
        _ => JsonSerializer.Serialize(value)
    };
}