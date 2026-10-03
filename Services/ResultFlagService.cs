using System.Globalization;
using System.Text.RegularExpressions;

namespace ZDLISPlus.Web.Services;

public static class ResultFlagService
{
    // Returns "H", "L", "N", or null when we can't determine.
    public static string? ComputeFlag(string? value, string? referenceRange, string? gender = null)
    {
        if (string.IsNullOrWhiteSpace(value) || string.IsNullOrWhiteSpace(referenceRange))
            return null;

        var v = value.Trim();

        // Qualitative — matches case-insensitively
        if (!TryParseNumber(v, out var num))
        {
            // "Negative" / "Normal" / "Clear" / "Yellow" — mark N if it matches the reference text
            var r = referenceRange.Trim().ToLowerInvariant();
            return r.Contains(v.ToLowerInvariant()) ? "N" : null;
        }

        var range = referenceRange.Trim();

        // "< X"  → anything >= X is High
        var lt = Regex.Match(range, @"^<\s*(-?\d+(?:\.\d+)?)");
        if (lt.Success && TryParseNumber(lt.Groups[1].Value, out var hi))
            return num >= hi ? "H" : "N";

        // "> X"  → anything <= X is Low
        var gt = Regex.Match(range, @"^>\s*(-?\d+(?:\.\d+)?)");
        if (gt.Success && TryParseNumber(gt.Groups[1].Value, out var lo))
            return num <= lo ? "L" : "N";

        // "A – B" or "A - B" (en dash, hyphen, em dash all supported)
        var rangeMatch = Regex.Match(range, @"(-?\d+(?:\.\d+)?)\s*[–\-—]\s*(-?\d+(?:\.\d+)?)");
        if (rangeMatch.Success
            && TryParseNumber(rangeMatch.Groups[1].Value, out var min)
            && TryParseNumber(rangeMatch.Groups[2].Value, out var max))
        {
            if (num < min) return "L";
            if (num > max) return "H";
            return "N";
        }

        return null;
    }

    private static bool TryParseNumber(string s, out double value)
        => double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out value);
}