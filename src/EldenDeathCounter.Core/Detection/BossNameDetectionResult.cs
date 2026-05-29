namespace EldenDeathCounter.Core.Detection;

/// <summary>An OCR boss-name candidate read from a single boss bar's name region, with its list match.</summary>
public sealed record BossNameCandidate(string RawText, BossNameMatch Match);

/// <summary>
/// The outcome of reading every detected boss bar's name region in one frame: how many bars were
/// present, how many produced a confident boss-list match, and the combined display name.
/// </summary>
public sealed record BossNameDetectionResult(
    int BarCount,
    int MatchedCount,
    string? CombinedName,
    IReadOnlyList<BossNameCandidate> Candidates)
{
    /// <summary>
    /// Builds a result from per-bar candidates (already ordered top-to-bottom). Only confidently
    /// matched names are combined with " + "; rejected/garbage candidates are dropped so corrupted
    /// OCR text can never reach the overlay. Identical display names are de-duplicated.
    /// </summary>
    public static BossNameDetectionResult FromMatches(int barCount, IReadOnlyList<BossNameCandidate> candidates)
    {
        var matched = candidates.Where(candidate => candidate.Match.IsMatch).ToList();

        var distinctNames = new List<string>();
        foreach (var candidate in matched)
        {
            var name = candidate.Match.DisplayName ?? candidate.Match.CanonicalName;
            if (string.IsNullOrWhiteSpace(name))
            {
                continue;
            }

            if (!distinctNames.Contains(name, StringComparer.CurrentCultureIgnoreCase))
            {
                distinctNames.Add(name);
            }
        }

        var combined = distinctNames.Count == 0 ? null : string.Join(" + ", distinctNames);
        return new BossNameDetectionResult(barCount, matched.Count, combined, candidates);
    }
}
