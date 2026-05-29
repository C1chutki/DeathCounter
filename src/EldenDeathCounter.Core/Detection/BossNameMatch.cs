namespace EldenDeathCounter.Core.Detection;

/// <summary>
/// Result of matching an OCR boss-name candidate against the configured boss list.
/// </summary>
/// <param name="IsMatch">Whether the candidate matched a boss list entry above the confidence threshold.</param>
/// <param name="CanonicalName">The matched boss list entry (clean, trusted spelling), or null when no match.</param>
/// <param name="DisplayName">The name to show to the user: the canonical name plus any clean parenthetical
/// disambiguator read from OCR (e.g. "Crystalian (Spear)"), or null when no match.</param>
/// <param name="Confidence">Similarity score in [0,1] for the best candidate (matched or not).</param>
public sealed record BossNameMatch(bool IsMatch, string? CanonicalName, string? DisplayName, double Confidence)
{
    public static BossNameMatch None { get; } = new(false, null, null, 0d);
}
