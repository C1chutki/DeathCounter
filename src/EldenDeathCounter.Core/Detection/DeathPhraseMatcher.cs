using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace EldenDeathCounter.Core.Detection;

public sealed class DeathPhraseMatcher
{
    private static readonly Regex WhitespaceRegex = new(@"\s+", RegexOptions.Compiled);

    public DeathPhraseMatch Match(
        string ocrText,
        IReadOnlyCollection<string> phrases,
        double sensitivity,
        bool suppressOwnSettingsTextGuard = false)
    {
        var normalizedText = Normalize(ocrText);
        if (string.IsNullOrWhiteSpace(normalizedText) || phrases.Count == 0)
        {
            return new DeathPhraseMatch(false, null, 0, normalizedText)
            {
                Details = string.IsNullOrWhiteSpace(normalizedText)
                    ? "reason=empty-normalized-ocr"
                    : "reason=no-configured-phrases"
            };
        }

        var normalizedPhrases = phrases
            .Where(phrase => !string.IsNullOrWhiteSpace(phrase))
            .Select(Normalize)
            .Where(phrase => phrase.Length > 0)
            .Distinct(StringComparer.Ordinal)
            .ToList();
        if (!suppressOwnSettingsTextGuard && LooksLikeOwnSettingsText(normalizedText, normalizedPhrases))
        {
            return new DeathPhraseMatch(false, null, 0, normalizedText)
            {
                Details = "reason=own-settings-window-text"
            };
        }

        var threshold = Math.Clamp(sensitivity, 0.1, 1.0);
        var candidates = BuildCandidates(normalizedText);
        string? bestPhrase = null;
        string? bestCandidate = null;
        var bestScore = 0d;

        foreach (var phrase in phrases.Where(phrase => !string.IsNullOrWhiteSpace(phrase)))
        {
            var normalizedPhrase = Normalize(phrase);
            if (normalizedText.Contains(normalizedPhrase, StringComparison.Ordinal))
            {
                return new DeathPhraseMatch(true, phrase, 1, normalizedText)
                {
                    Details = $"reason=contains-normalized-phrase; threshold={FormatScore(threshold)}; normalizedPhrase='{normalizedPhrase}'"
                };
            }

            foreach (var candidate in candidates)
            {
                var score = Similarity(candidate, normalizedPhrase);
                if (score > bestScore)
                {
                    bestScore = score;
                    bestPhrase = phrase;
                    bestCandidate = candidate;
                }
            }
        }

        return new DeathPhraseMatch(bestScore >= threshold, bestPhrase, bestScore, normalizedText)
        {
            Details =
                $"reason={(bestScore >= threshold ? "score-meets-threshold" : "score-below-threshold")}; " +
                $"threshold={FormatScore(threshold)}; bestCandidate='{bestCandidate ?? ""}'; candidateCount={candidates.Count}"
        };
    }

    public static string Normalize(string value)
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

    private static IReadOnlyCollection<string> BuildCandidates(string normalizedText)
    {
        var tokens = normalizedText.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var candidates = new HashSet<string>(StringComparer.Ordinal) { normalizedText };

        for (var length = 1; length <= Math.Min(tokens.Length, 6); length++)
        {
            for (var start = 0; start <= tokens.Length - length; start++)
            {
                candidates.Add(string.Join(' ', tokens.Skip(start).Take(length)));
            }
        }

        return candidates;
    }

    private static bool LooksLikeOwnSettingsText(string normalizedText, IReadOnlyCollection<string> normalizedPhrases)
    {
        var phraseHits = normalizedPhrases.Count(phrase => normalizedText.Contains(phrase, StringComparison.Ordinal));
        if (phraseHits >= 2)
        {
            return true;
        }

        return normalizedText.Contains("DETECTION PHRASES", StringComparison.Ordinal) ||
               normalizedText.Contains("START DETECTION", StringComparison.Ordinal) ||
               normalizedText.Contains("STOP DETECTION", StringComparison.Ordinal) ||
               normalizedText.Contains("DETECTION SENSITIVITY", StringComparison.Ordinal);
    }

    private static double Similarity(string left, string right)
    {
        if (left.Length == 0 && right.Length == 0)
        {
            return 1;
        }

        if (left.Length == 0 || right.Length == 0)
        {
            return 0;
        }

        var distance = LevenshteinDistance(left, right);
        var max = Math.Max(left.Length, right.Length);
        return 1d - (double)distance / max;
    }

    private static int LevenshteinDistance(string left, string right)
    {
        var previous = new int[right.Length + 1];
        var current = new int[right.Length + 1];

        for (var i = 0; i <= right.Length; i++)
        {
            previous[i] = i;
        }

        for (var i = 1; i <= left.Length; i++)
        {
            current[0] = i;
            for (var j = 1; j <= right.Length; j++)
            {
                var cost = left[i - 1] == right[j - 1] ? 0 : 1;
                current[j] = Math.Min(
                    Math.Min(current[j - 1] + 1, previous[j] + 1),
                    previous[j - 1] + cost);
            }

            (previous, current) = (current, previous);
        }

        return previous[right.Length];
    }

    private static string FormatScore(double value)
    {
        return value.ToString("0.000", CultureInfo.InvariantCulture);
    }
}
