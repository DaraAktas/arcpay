using System.Text.RegularExpressions;

namespace ArcPay.CustomerApi.Services;

public static partial class PhoneNumberNormalizer
{
    public static bool TryNormalize(string? value, out string normalized)
    {
        normalized = string.Empty;
        if (string.IsNullOrWhiteSpace(value)) return false;

        var compact = SeparatorRegex().Replace(value.Trim(), string.Empty);
        if (compact.StartsWith("00", StringComparison.Ordinal)) compact = $"+{compact[2..]}";
        if (compact.Length == 11 && compact.StartsWith("05", StringComparison.Ordinal)) compact = $"+9{compact}";
        if (compact.Length == 10 && compact.StartsWith('5')) compact = $"+90{compact}";

        if (!InternationalPhoneRegex().IsMatch(compact)) return false;
        normalized = compact;
        return true;
    }

    [GeneratedRegex("[\\s()\\-]")]
    private static partial Regex SeparatorRegex();

    [GeneratedRegex("^\\+[1-9][0-9]{9,14}$")]
    private static partial Regex InternationalPhoneRegex();
}
