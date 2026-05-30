namespace EldenDeathCounter.Core.Configuration;

public sealed class AppSettings
{
    public bool OverlayEnabled { get; set; } = true;

    public double OverlayX { get; set; } = 40;

    public double OverlayY { get; set; } = 40;

    public bool DetectionEnabledOnStartup { get; set; }

    public bool AutoDetectBossNames { get; set; } = true;

    public string GameLanguage { get; set; } = "PL";

    public Dictionary<string, string> BossNameCorrections { get; set; } = [];

    public int DetectionIntervalMs { get; set; } = 300;

    public int DetectionCooldownSeconds { get; set; } = 25;

    public double DetectionSensitivity { get; set; } = 0.8;

    public string CaptureTarget { get; set; } = "EldenRingWindow";

    public string DataFolderPath { get; set; } = string.Empty;

    public DiagnosticsMode DiagnosticsMode { get; set; } = DiagnosticsMode.Events;

    public int DiagnosticsSessionMinutes { get; set; } = 10;

    public int DiagnosticsMaxEventLogMb { get; set; } = 5;

    public int DiagnosticsRetentionDays { get; set; } = 7;

    public List<string> DetectionPhrases { get; set; } = [];

    public List<string> BossVictoryPhrases { get; set; } = [];

    public string ManualAddHotkey { get; set; } = "F8";

    public string ManualSubtractHotkey { get; set; } = "F9";

    public string BossDefeatedHotkey { get; set; } = "F7";

    public string OverlayToggleHotkey { get; set; } = "Ctrl+Shift+O";

    public double OverlayFontScale { get; set; } = 1.0;

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
            GameLanguage = "PL",
            DetectionIntervalMs = 300,
            DetectionCooldownSeconds = 25,
            DetectionSensitivity = 0.8,
            CaptureTarget = "EldenRingWindow",
            DataFolderPath = profile.GetDataFolderPath(desktopPath),
            DiagnosticsMode = DiagnosticsMode.Events,
            DiagnosticsSessionMinutes = 10,
            DiagnosticsMaxEventLogMb = 5,
            DiagnosticsRetentionDays = 7,
            DetectionPhrases = ["YOU DIED", "NIE ŻYJESZ"],
            BossVictoryPhrases =
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
            ],
            BossNameCorrections = CreateDefaultBossNameCorrections(),
            ManualAddHotkey = "F8",
            ManualSubtractHotkey = "F9",
            BossDefeatedHotkey = "F7",
            OverlayToggleHotkey = "Ctrl+Shift+O",
            OverlayFontScale = 1.0
        };
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
