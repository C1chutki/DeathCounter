namespace EldenDeathCounter.Core.Detection;

public sealed record DeathPhraseMatch(
    bool IsMatch,
    string? MatchedPhrase,
    double Score,
    string NormalizedText)
{
    public string Details { get; init; } = string.Empty;
}
