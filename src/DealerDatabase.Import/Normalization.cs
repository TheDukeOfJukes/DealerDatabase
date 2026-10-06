using System.Text.RegularExpressions;

namespace DealerDatabase.Import;

/// <summary>
/// Normalizes dealer identifiers and contact details for matching and consolidation.
/// </summary>
internal static class Normalization
{
    private static readonly Regex CrnSuffix = new(@"\.0$", RegexOptions.CultureInvariant | RegexOptions.Compiled);
    private static readonly Regex InvalidNameCharacters = new("[^a-z0-9\\s]", RegexOptions.CultureInvariant | RegexOptions.Compiled);
    private static readonly Regex NameSuffixes = new(@"\b(ltd|limited|uk|co|company)\b", RegexOptions.CultureInvariant | RegexOptions.Compiled);
    private static readonly Regex MultipleSpaces = new(@"\s+", RegexOptions.CultureInvariant | RegexOptions.Compiled);
    private static readonly Regex RepeatedCommas = new(@"(?:,\s*){2,}", RegexOptions.CultureInvariant | RegexOptions.Compiled);
    private static readonly Regex SpacesAroundCommas = new(@"\s*,\s*", RegexOptions.CultureInvariant | RegexOptions.Compiled);
    private static readonly Regex PhoneExtension = new(@"(?:\bext(?:ension)?\.?|\bx\b|#)\s*\d+\s*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);
    private static readonly Regex UrlScheme = new(@"^[a-z][a-z\d+.-]*://", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

    internal static string? NormalizeCrn(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return null;
        }

        // Spreadsheet exports may append .0 to numeric identifiers; restore the fixed-width form after removing it.
        var value = CrnSuffix.Replace(input.Trim(), string.Empty);
        value = string.Concat(value.Where(character => !char.IsWhiteSpace(character))).ToUpperInvariant();
        return value.Length == 0 ? null : value.PadLeft(8, '0');
    }

    internal static string? NormalizePostcode(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return null;
        }

        var value = string.Concat(input.Where(character => !char.IsWhiteSpace(character))).ToUpperInvariant();
        return value.Length == 0 ? null : value;
    }

    internal static string? NormalizeName(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return null;
        }

        var value = InvalidNameCharacters.Replace(input.ToLowerInvariant(), string.Empty);
        value = NameSuffixes.Replace(value, " ");
        value = MultipleSpaces.Replace(value, " ").Trim();
        return value.Length == 0 ? null : value;
    }

    internal static string? NormalizeAddress(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return null;
        }

        var value = MultipleSpaces.Replace(input.Trim(), " ");
        value = RepeatedCommas.Replace(value, ",");
        value = SpacesAroundCommas.Replace(value, ", ").Trim(' ', ',');
        return value.Length == 0 ? null : value;
    }

    internal static string? NormalizePhone(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return null;
        }

        var value = MultipleSpaces.Replace(input.Trim(), " ");
        if (PhoneExtension.IsMatch(value))
        {
            // Keep numbers with extensions intact rather than dropping the extension during digit-only normalization.
            return value;
        }

        var digits = string.Concat(value.Where(char.IsAsciiDigit));
        var hasInternationalPrefix = value.StartsWith('+') || digits.StartsWith("00", StringComparison.Ordinal);
        if (digits.StartsWith("00", StringComparison.Ordinal))
        {
            digits = digits[2..];
        }

        if (digits.StartsWith("44", StringComparison.Ordinal))
        {
            var nationalNumber = digits[2..].TrimStart('0');
            return nationalNumber.Length is >= 9 and <= 12 ? $"+44{nationalNumber}" : value;
        }

        if (hasInternationalPrefix && digits.Length is >= 8 and <= 15)
        {
            return $"+{digits}";
        }

        if (!hasInternationalPrefix && digits.StartsWith('0') && digits.Length is >= 10 and <= 11)
        {
            return $"+44{digits[1..]}";
        }

        return value;
    }

    internal static string? NormalizeWebsite(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return null;
        }

        var value = input.Trim().TrimEnd(',', ';');
        if (value.StartsWith("//", StringComparison.Ordinal))
        {
            value = $"https:{value}";
        }
        else if (!UrlScheme.IsMatch(value))
        {
            value = $"https://{value}";
        }

        if (!Uri.TryCreate(value, UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        {
            return input.Trim();
        }

        var domain = NormalizeDomain(uri.IdnHost);
        if (domain is null)
        {
            return input.Trim();
        }

        var builder = new UriBuilder(uri)
        {
            Host = domain,
            Fragment = string.Empty
        };
        var normalized = builder.Uri.GetLeftPart(UriPartial.Path);
        if (builder.Path == "/" && string.IsNullOrEmpty(builder.Query))
        {
            normalized = normalized.TrimEnd('/');
        }

        return normalized + builder.Query;
    }

    private static string? NormalizeDomain(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return null;
        }

        var domain = input.Trim().TrimEnd('.').ToLowerInvariant();
        return domain.Length == 0 ? null : domain;
    }
}