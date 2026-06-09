using System.Text.Json.Serialization;

namespace EldenDeathCounter.Core.Configuration;

public sealed class AppSettings
{
    public bool OverlayEnabled { get; set; } = true;

    public double OverlayX { get; set; } = 40;

    public double OverlayY { get; set; } = 40;

    public bool DetectionEnabledOnStartup { get; set; }

    public bool AutoDetectBossNames { get; set; } = true;

    public string GameLanguage { get; set; } = "PL";

    // Active game profile id (EldenRing/DarkSouls1/DarkSouls2/DarkSouls3). Derived from the loaded
    // profile by AppSettingsStore, never read from disk, so a stale JSON value can't select the
    // wrong game's detection assets (boss lists, death/victory templates).
    [JsonIgnore]
    public string GameId { get; set; } = AppGameProfile.EldenRing.Id;

    // UI language for the app chrome (en/pl). Independent of GameLanguage (OCR/detection).
    public string AppLanguage { get; set; } = "en";

    public Dictionary<string, string> BossNameCorrections { get; set; } = [];

    public int DetectionIntervalMs { get; set; } = 300;

    public int DetectionCooldownSeconds { get; set; } = 25;

    public double DetectionSensitivity { get; set; } = 0.8;

    public string CaptureTarget { get; set; } = "EldenRingWindow";

    public string DataFolderPath { get; set; } = string.Empty;

    public string CharacterProfileName { get; set; } = string.Empty;

    public DiagnosticsMode DiagnosticsMode { get; set; } = DiagnosticsMode.Events;

    public int DiagnosticsSessionMinutes { get; set; } = 10;

    public int DiagnosticsMaxEventLogMb { get; set; } = 5;

    public int DiagnosticsRetentionDays { get; set; } = 7;

    [JsonIgnore]
    public List<string> DetectionPhrases { get; set; } = CreateDefaultDetectionPhrases();

    [JsonIgnore]
    public List<string> BossVictoryPhrases { get; set; } = CreateDefaultBossVictoryPhrases();

    public string ManualAddHotkey { get; set; } = "F9";

    public string ManualSubtractHotkey { get; set; } = "F8";

    public string BossDefeatedHotkey { get; set; } = "F7";

    public string OverlayToggleHotkey { get; set; } = "Ctrl+Shift+O";

    public string DetectionToggleHotkey { get; set; } = "F6";

    public string BossSkipHotkey { get; set; } = "Ctrl+Shift+P";

    // Scale of the whole overlay window (text, spacing, borders, divider, timer).
    // Kept under the original "OverlayFontScale" name so existing settings JSON keeps loading.
    public double OverlayFontScale { get; set; } = 1.0;

    // Opacity of the overlay background only (0.0–1.0). Text stays fully opaque.
    // Default 0.9 matches the previous look (background alpha 0xE6 ≈ 0.90).
    public double OverlayBackgroundOpacity { get; set; } = 0.9;

    public static AppSettings CreateDefault(string desktopPath)
    {
        return CreateDefault(desktopPath, AppGameProfile.EldenRing);
    }

    public static AppSettings CreateDefault(string desktopPath, AppGameProfile profile)
    {
        return new AppSettings
        {
            OverlayEnabled = true,
            OverlayX = 40,
            OverlayY = 40,
            DetectionEnabledOnStartup = false,
            AutoDetectBossNames = true,
            GameId = profile.Id,
            GameLanguage = "PL",
            AppLanguage = "en",
            DetectionIntervalMs = 300,
            DetectionCooldownSeconds = 25,
            DetectionSensitivity = 0.8,
            CaptureTarget = "EldenRingWindow",
            DataFolderPath = profile.GetDataFolderPath(desktopPath),
            CharacterProfileName = string.Empty,
            DiagnosticsMode = DiagnosticsMode.Events,
            DiagnosticsSessionMinutes = 10,
            DiagnosticsMaxEventLogMb = 5,
            DiagnosticsRetentionDays = 7,
            DetectionPhrases = CreateDefaultDetectionPhrases(),
            BossVictoryPhrases = CreateDefaultBossVictoryPhrases(),
            BossNameCorrections = CreateDefaultBossNameCorrections(),
            ManualAddHotkey = "F9",
            ManualSubtractHotkey = "F8",
            BossDefeatedHotkey = "F7",
            OverlayToggleHotkey = "Ctrl+Shift+O",
            DetectionToggleHotkey = "F6",
            BossSkipHotkey = "Ctrl+Shift+P",
            OverlayFontScale = 1.0,
            OverlayBackgroundOpacity = 0.9
        };
    }

    public static List<string> CreateDefaultDetectionPhrases()
    {
        return ["YOU DIED", "NIE ŻYJESZ"];
    }

    public static List<string> CreateDefaultBossVictoryPhrases()
    {
        return
        [
            "POKONANO WROGA",
            "POKONANO WIELKIEGO WROGA",
            "POKONANO LEGENDE",
            "POKONANO POLBOGA",
            "ZABITO BOGA",
            "WRÓG POWALONY",
            "WIELKI WRÓG POWALONY",
            "ENEMY FELLED",
            "GREAT ENEMY FELLED",
            "LEGEND FELLED",
            "DEMIGOD FELLED",
            "GOD SLAIN"
        ];
    }

    private static Dictionary<string, string> CreateDefaultBossNameCorrections()
    {
        return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["STRAINIK DRZEWA"] = "Strażnik Drzewa",
            ["STRAZNIK DRZEWA"] = "Strażnik Drzewa",
            ["STRONIK DRZEWA"] = "Strażnik Drzewa",
            ["STRONIK DRIEWA"] = "Strażnik Drzewa",
            ["STRAILFLK RZEWA"] = "Strażnik Drzewa",
            ["STRAIFLK RZEWA"] = "Strażnik Drzewa",
            ["STRAILFLK DRZEWA"] = "Strażnik Drzewa"
        };
    }
}
