using System.IO;

namespace EldenDeathCounter.Core.Detection;

/// <summary>
/// Resolves which reference screenshots to build death- and boss-victory text templates from,
/// per game and language. File names are relative to the <c>Assets</c> folder.
///
/// Dark Souls III shows its death banner in English ("YOU DIED") even when the game/app is set to
/// Polish, so DS3 always loads both language templates regardless of the configured language. This
/// gives the stabilizer's near-threshold image confirmation a template to match on the death-screen
/// fade frames (where OCR alone often catches only a single frame, below the 2-frame confirmation).
/// </summary>
public static class GameDeathScreenTemplates
{
    private const string DarkSouls3FolderName = "Dark souls 3";
    private const string EldenRingFolderName = "Elden Ring";

    public static IReadOnlyList<string> DeathTemplateFiles(string gameId, string language)
    {
        if (IsDarkSouls3(gameId))
        {
            return
            [
                Ds3("PL_YouDied.jpg"),
                Ds3("PL_YouDied_v2.jpg"),
                Ds3("ENG_YouDied.jpg")
            ];
        }

        return IsPolish(language)
            ? [Er("PL_Death_Screen.png"), Er("PL_Death_Screen_v2.jpg"), Er("PL_Death_Screen_v3.jpg")]
            : [
                Er("ENG_Death_Screen_v9.png"),
                Er("ENG_Death_Screen_v3.png"),
                Er("ENG_Death_Screen.png"),
                Er("ENG_Death_Screen_v8.png"),
                Er("ENG_Death_Screen_v7.png"),
                Er("ENG_Death_Screen_v11.png")
            ];
    }

    public static IReadOnlyList<string> VictoryTemplateFiles(string gameId, string language)
    {
        if (IsDarkSouls3(gameId))
        {
            return
            [
                Ds3("PL_Victory.jpg"),
                Ds3("PL_Victory_v2.jpg")
            ];
        }

        return IsPolish(language)
            ? [Er("PL_Win_screen.jpg")]
            : [Er("ENG_Win_Screen.jpg"), Er("ENG_Win_Screen_v2.jpg")];
    }

    private static string Ds3(string file) => Path.Combine(DarkSouls3FolderName, file);

    private static string Er(string file) => Path.Combine(EldenRingFolderName, file);

    private static bool IsDarkSouls3(string? gameId) =>
        string.Equals(gameId?.Trim(), "DarkSouls3", StringComparison.OrdinalIgnoreCase);

    private static bool IsPolish(string? language) =>
        (language?.Trim().ToUpperInvariant() ?? "PL") is not ("ENG" or "EN");
}
