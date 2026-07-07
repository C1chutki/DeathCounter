using System.IO;

namespace EldenDeathCounter.Core.Detection;

/// <summary>
/// Resolves which boss-name list file to load per game and language. File names are relative to the
/// <c>Assets</c> folder.
///
/// The detection pipeline was historically Elden-Ring-only and always loaded
/// <c>PL_BossList.txt</c>/<c>ENG_BossList.txt</c>, so Dark Souls boss names never matched even though
/// the <c>*_DS1/2/3_BossList.txt</c> assets exist. Routing by game id fixes per-boss tracking for all
/// four games from a single place.
///
/// The Elden Ring boss lists live in the <c>Assets/Elden Ring</c> subfolder (named
/// <c>*_ER_BossList.txt</c>); the Dark Souls lists remain at the <c>Assets</c> root.
/// </summary>
public static class GameBossListFiles
{
    private const string EldenRingFolderName = "Elden Ring";
    private const string ConvergenceFolderName = "Convergence";

    public static string Resolve(string? gameId, string? language)
    {
        var prefix = IsPolish(language) ? "PL" : "ENG";
        return Normalize(gameId) switch
        {
            "DARKSOULS1" => $"{prefix}_DS1_BossList.txt",
            "DARKSOULS2" => $"{prefix}_DS2_BossList.txt",
            "DARKSOULS3" => $"{prefix}_DS3_BossList.txt",
            _ => Path.Combine(EldenRingFolderName, $"{prefix}_ER_BossList.txt")
        };
    }

    public static IReadOnlyList<string> ResolveForMatcher(string? gameId, string? language, string? bossHealthBarStyle)
    {
        var primary = Resolve(gameId, language);
        if (Normalize(gameId) is not ("" or "ELDENRING"))
        {
            return [primary];
        }

        return BossHealthBarStyles.Normalize(bossHealthBarStyle) switch
        {
            BossHealthBarStyles.Reforged when IsPolish(language) => [primary, Resolve(gameId, "ENG")],
            BossHealthBarStyles.Convergence when IsPolish(language) => [primary, Resolve(gameId, "ENG"), ConvergenceBossList()],
            BossHealthBarStyles.Convergence => [primary, ConvergenceBossList()],
            _ => [primary]
        };
    }

    private static string ConvergenceBossList() =>
        Path.Combine(EldenRingFolderName, ConvergenceFolderName, "ENG_ER_Convergence_BossList.txt");

    private static string Normalize(string? gameId) =>
        (gameId?.Trim() ?? string.Empty).ToUpperInvariant();

    private static bool IsPolish(string? language) =>
        (language?.Trim() ?? "PL").StartsWith("PL", StringComparison.OrdinalIgnoreCase);
}
