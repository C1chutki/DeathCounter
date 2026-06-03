namespace EldenDeathCounter.Core.Detection;

/// <summary>
/// Resolves which boss-name list file to load per game and language. File names are relative to the
/// <c>Assets</c> folder.
///
/// The detection pipeline was historically Elden-Ring-only and always loaded
/// <c>PL_BossList.txt</c>/<c>ENG_BossList.txt</c>, so Dark Souls boss names never matched even though
/// the <c>*_DS1/2/3_BossList.txt</c> assets exist. Routing by game id fixes per-boss tracking for all
/// four games from a single place.
/// </summary>
public static class GameBossListFiles
{
    public static string Resolve(string? gameId, string? language)
    {
        var suffix = GameSuffix(gameId);
        return IsPolish(language)
            ? $"PL{suffix}_BossList.txt"
            : $"ENG{suffix}_BossList.txt";
    }

    private static string GameSuffix(string? gameId) =>
        (gameId?.Trim() ?? string.Empty).ToUpperInvariant() switch
        {
            "DARKSOULS1" => "_DS1",
            "DARKSOULS2" => "_DS2",
            "DARKSOULS3" => "_DS3",
            _ => string.Empty
        };

    private static bool IsPolish(string? language) =>
        (language?.Trim() ?? "PL").StartsWith("PL", StringComparison.OrdinalIgnoreCase);
}
