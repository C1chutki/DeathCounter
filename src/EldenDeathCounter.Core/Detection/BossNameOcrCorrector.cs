using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace EldenDeathCounter.Core.Detection;

public static class BossNameOcrCorrector
{
    private static readonly Regex WhitespaceRegex = new(@"\s+", RegexOptions.Compiled);

    public static string Apply(string ocrName, IReadOnlyDictionary<string, string> corrections)
    {
        var trimmed = WhitespaceRegex.Replace(ocrName.Trim(), " ");
        if (trimmed.Length == 0 || corrections.Count == 0)
        {
            return trimmed;
        }

        var key = NormalizeKey(trimmed);
        return corrections.TryGetValue(key, out var corrected)
            ? corrected
            : trimmed;
    }

    internal static string NormalizeKey(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var decomposed = value.Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(decomposed.Length);
        foreach (var character in decomposed)
        {
            var category = CharUnicodeInfo.GetUnicodeCategory(character);
            if (category == UnicodeCategory.NonSpacingMark)
            {
                continue;
            }

            builder.Append(char.IsLetterOrDigit(character) ? char.ToUpperInvariant(character) : ' ');
        }

        return WhitespaceRegex.Replace(builder.ToString(), " ").Trim();
    }
}
