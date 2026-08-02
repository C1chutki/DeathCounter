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
    private const string DarkSouls2FolderName = "Dark souls 2";
    private const string EldenRingFolderName = "Elden Ring";
    private const string SekiroFolderName = "Sekiro";

    public static IReadOnlyList<string> DeathTemplateFiles(string gameId, string language) =>
        DeathTemplateFiles(gameId, language, BossHealthBarStyles.Vanilla);

    public static IReadOnlyList<string> DeathTemplateFiles(string gameId, string language, string? bossHealthBarStyle)
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

        if (IsDarkSouls2(gameId))
        {
            // Dark Souls II renders "YOU DIED" in English regardless of language, and we only have the
            // English reference, so DS2 always uses it and never falls back to the Elden Ring screens.
            return [Ds2("ENG_YouDied.jpg")];
        }

        if (IsSekiro(gameId))
        {
            // Sekiro's death screen is the red 死 kanji with a spaced "D E A T H" beneath it, identical
            // in every language. Multiple fade stages cover its changing brightness and glow.
            return
            [
                Se("ENG_Death_Screen.png"),
                Se("ENG_Death_Screen_v2.png"),
                Se("ENG_Death_Screen_v3.png"),
                Se("ENG_Death_Screen_v4.png")
            ];
        }

        IReadOnlyList<string> files = IsPolish(language)
            ? [Er("PL_Death_Screen.png"), Er("PL_Death_Screen_v2.jpg"), Er("PL_Death_Screen_v3.jpg")]
            : [
                Er("ENG_Death_Screen_v9.png"),
                Er("ENG_Death_Screen_v3.png"),
                Er("ENG_Death_Screen.png"),
                Er("ENG_Death_Screen_v8.png"),
                Er("ENG_Death_Screen_v7.png"),
                Er("ENG_Death_Screen_v11.png")
            ];

        return BossHealthBarStyles.Normalize(bossHealthBarStyle) == BossHealthBarStyles.Reforged
            ? [Er(Path.Combine("Reforge", "YouDied_Reforge.png")), .. files]
            : files;
    }

    public static IReadOnlyList<string> VictoryTemplateFiles(string gameId, string language)
    {
        if (IsDarkSouls2(gameId))
        {
            // No DS2 victory reference asset exists, so DS2 has no victory template (never Elden Ring's).
            return [];
        }

        if (IsSekiro(gameId))
        {
            // Sekiro's boss kill shows only the 忍殺 kanji (no Latin text), so there is no victory
            // template or phrase to match; boss victories are recorded from the bar disappearing or
            // from the manual hotkey.
            return [];
        }

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

    private static string Ds2(string file) => Path.Combine(DarkSouls2FolderName, file);

    private static string Er(string file) => Path.Combine(EldenRingFolderName, file);

    private static string Se(string file) => Path.Combine(SekiroFolderName, file);

    public static bool IsSekiro(string? gameId) =>
        string.Equals(gameId?.Trim(), "Sekiro", StringComparison.OrdinalIgnoreCase);

    private static bool IsDarkSouls3(string? gameId) =>
        string.Equals(gameId?.Trim(), "DarkSouls3", StringComparison.OrdinalIgnoreCase);

    private static bool IsDarkSouls2(string? gameId) =>
        string.Equals(gameId?.Trim(), "DarkSouls2", StringComparison.OrdinalIgnoreCase);

    private static bool IsPolish(string? language) =>
        (language?.Trim().ToUpperInvariant() ?? "PL") is not ("ENG" or "EN");
}
