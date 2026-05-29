using System.Text.RegularExpressions;

namespace EldenDeathCounter.Core.Detection;

/// <summary>
/// Validates OCR boss-name candidates against a known boss list using normalization plus a
/// fuzzy (edit-distance) similarity threshold. This is the gate that prevents corrupted OCR
/// text and ordinary HUD/interaction prompts (e.g. "Talk", "Sit", "Bloody Slash") from ever
/// becoming the active boss name.
/// </summary>
public sealed partial class BossNameMatcher
{
    private readonly IReadOnlyList<(string Canonical, string Key)> _entries;
    private readonly double _confidenceThreshold;

    public BossNameMatcher(IReadOnlyList<string> bossNames, double confidenceThreshold = 0.82)
    {
        _confidenceThreshold = confidenceThreshold;
        _entries = bossNames
            .Select(name => (Canonical: name.Trim(), Key: BossNameOcrCorrector.NormalizeKey(StripParentheticals(name))))
            .Where(entry => entry.Key.Length > 0)
            .ToList();
    }

    public bool HasEntries => _entries.Count > 0;

    public int Count => _entries.Count;

    public double ConfidenceThreshold => _confidenceThreshold;

    public BossNameMatch Match(string ocrText)
    {
        if (string.IsNullOrWhiteSpace(ocrText) || !HasEntries)
        {
            return BossNameMatch.None;
        }

        var candidateKey = BossNameOcrCorrector.NormalizeKey(StripParentheticals(ocrText));
        if (candidateKey.Length == 0)
        {
            return BossNameMatch.None;
        }

        var bestScore = 0d;
        string? bestCanonical = null;
        foreach (var entry in _entries)
        {
            var score = Similarity(candidateKey, entry.Key);
            if (score > bestScore)
            {
                bestScore = score;
                bestCanonical = entry.Canonical;
            }

            if (bestScore >= 1d)
            {
                break;
            }
        }

        if (bestCanonical is null || bestScore < _confidenceThreshold)
        {
            return new BossNameMatch(false, null, null, bestScore);
        }

        var displayName = ComposeDisplayName(bestCanonical, ocrText);
        return new BossNameMatch(true, bestCanonical, displayName, bestScore);
    }

    public static IReadOnlyList<string> ParseList(IEnumerable<string> lines)
    {
        return lines
            .Select(line => line.Trim())
            .Where(line => line.Length > 0 && !line.StartsWith('#'))
            .ToList();
    }

    /// <summary>
    /// Keeps a clean parenthetical disambiguator (e.g. "(Spear)") on the trusted canonical name so
    /// multi-boss encounters can be shown as "Crystalian (Spear) + Crystalian (Staff)". The base
    /// spelling always comes from the boss list, never from raw OCR.
    /// </summary>
    private static string ComposeDisplayName(string canonical, string ocrText)
    {
        var match = ParentheticalRegex().Match(ocrText);
        if (!match.Success)
        {
            return canonical;
        }

        var inner = new string(match.Groups[1].Value
            .Where(c => char.IsLetter(c) || char.IsWhiteSpace(c))
            .ToArray());
        inner = WhitespaceRegex().Replace(inner, " ").Trim();
        return inner.Length is > 0 and <= 24 ? $"{canonical} ({inner})" : canonical;
    }

    private static string StripParentheticals(string value)
    {
        return ParentheticalRegex().Replace(value, " ");
    }

    private static double Similarity(string a, string b)
    {
        if (a.Length == 0 && b.Length == 0)
        {
            return 1d;
        }

        var distance = LevenshteinDistance(a, b);
        var maxLength = Math.Max(a.Length, b.Length);
        return maxLength == 0 ? 0d : 1d - (double)distance / maxLength;
    }

    private static int LevenshteinDistance(string a, string b)
    {
        var previous = new int[b.Length + 1];
        var current = new int[b.Length + 1];
        for (var j = 0; j <= b.Length; j++)
        {
            previous[j] = j;
        }

        for (var i = 1; i <= a.Length; i++)
        {
            current[0] = i;
            for (var j = 1; j <= b.Length; j++)
            {
                var cost = a[i - 1] == b[j - 1] ? 0 : 1;
                current[j] = Math.Min(
                    Math.Min(current[j - 1] + 1, previous[j] + 1),
                    previous[j - 1] + cost);
            }

            (previous, current) = (current, previous);
        }

        return previous[b.Length];
    }

    [GeneratedRegex(@"\(([^)]*)\)", RegexOptions.Compiled)]
    private static partial Regex ParentheticalRegex();

    [GeneratedRegex(@"\s+", RegexOptions.Compiled)]
    private static partial Regex WhitespaceRegex();
}
