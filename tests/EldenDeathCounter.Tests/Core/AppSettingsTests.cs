using EldenDeathCounter.Core.Configuration;
using EldenDeathCounter.Core.Detection;
using EldenDeathCounter.Core.Logging;
using EldenDeathCounter.Core.Storage;

namespace EldenDeathCounter.Tests.Core;

public sealed class AppSettingsTests
{
    [Fact]
    public void DefaultsPointAtDesktopFolderAndIncludeRequiredPhrases()
    {
        var settings = AppSettings.CreateDefault(@"C:\Users\TestUser\Desktop");

        Assert.Equal(@"C:\Users\TestUser\Desktop\DeathCounter\EldenRing", settings.DataFolderPath);
        Assert.True(settings.OverlayEnabled);
        Assert.True(settings.AutoDetectBossNames);
        Assert.Equal(350, settings.DetectionIntervalMs);
        Assert.Equal(25, settings.DetectionCooldownSeconds);
        Assert.Equal(0.8, settings.DetectionSensitivity);
        Assert.Equal("Balanced", settings.DetectionMode);
        Assert.True(settings.DetectDeaths);
        Assert.True(settings.DetectBossVictories);
        Assert.True(settings.ShowBossTimer);
        Assert.True(settings.ShowDetectionStatus);
        Assert.Equal(BossHealthBarStyles.Vanilla, settings.BossHealthBarStyle);
        Assert.Equal("EldenRingWindow", settings.CaptureTarget);
        Assert.Equal(DiagnosticsMode.Events, settings.DiagnosticsMode);
        Assert.Equal(10, settings.DiagnosticsSessionMinutes);
        Assert.Equal(5, settings.DiagnosticsMaxEventLogMb);
        Assert.Equal(7, settings.DiagnosticsRetentionDays);
        Assert.Equal("PL", settings.GameLanguage);
        Assert.Contains("YOU DIED", settings.DetectionPhrases);
        Assert.Contains("NIE ŻYJESZ", settings.DetectionPhrases);
        Assert.Contains("POKONANO WROGA", settings.BossVictoryPhrases);
        Assert.Contains("POKONANO WIELKIEGO WROGA", settings.BossVictoryPhrases);
        Assert.Contains("POKONANO LEGENDE", settings.BossVictoryPhrases);
        Assert.Contains("POKONANO POLBOGA", settings.BossVictoryPhrases);
        Assert.Contains("ZABITO BOGA", settings.BossVictoryPhrases);
        Assert.Contains("ENEMY FELLED", settings.BossVictoryPhrases);
        Assert.Contains("GOD SLAIN", settings.BossVictoryPhrases);
        Assert.Equal("F9", settings.ManualAddHotkey);
        Assert.Equal("F8", settings.ManualSubtractHotkey);
        Assert.Equal("F7", settings.BossDefeatedHotkey);
        Assert.Equal("Ctrl+Shift+O", settings.OverlayToggleHotkey);
        Assert.Equal("F6", settings.DetectionToggleHotkey);
        Assert.Equal("Ctrl+Shift+P", settings.BossSkipHotkey);
        Assert.Equal(1.0, settings.OverlayFontScale);
        Assert.Equal(0.9, settings.OverlayBackgroundOpacity);
        Assert.Equal(string.Empty, settings.CharacterProfileName);
        Assert.Equal("Strażnik Drzewa", settings.BossNameCorrections["STRAINIK DRZEWA"]);
    }

    [Fact]
    public void DefaultGameIdMatchesTheRequestedProfile()
    {
        var settings = AppSettings.CreateDefault(@"C:\Users\TestUser\Desktop", AppGameProfile.DarkSouls3);

        Assert.Equal("DarkSouls3", settings.GameId);
        Assert.Equal("DarkSouls3Window", settings.CaptureTarget);
    }

    [Fact]
    public async Task LoadingLegacyDarkSoulsSettingsReplacesTheOldEldenRingWindowTarget()
    {
        var folder = Path.Combine(Path.GetTempPath(), "EldenDeathCounterTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        var settingsPath = Path.Combine(folder, "appsettings.json");
        await File.WriteAllTextAsync(settingsPath, """{"captureTarget":"EldenRingWindow"}""");
        var store = new AppSettingsStore(new FileLogService(Path.Combine(folder, "log.txt")));

        var settings = await store.LoadAsync(settingsPath, @"C:\Users\TestUser\Desktop", AppGameProfile.DarkSouls3);

        Assert.Equal("DarkSouls3Window", settings.CaptureTarget);
    }

    [Fact]
    public async Task LoadingSettingsStampsGameIdFromProfileAndIgnoresDiskValue()
    {
        var folder = Path.Combine(Path.GetTempPath(), "EldenDeathCounterTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        var settingsPath = Path.Combine(folder, "appsettings.json");
        // A stale/hand-edited gameId on disk must never select the wrong game's detection assets.
        await File.WriteAllTextAsync(settingsPath, """{"dataFolderPath":"C:\\Temp\\Counter","gameId":"EldenRing"}""");
        var store = new AppSettingsStore(new FileLogService(Path.Combine(folder, "log.txt")));

        var settings = await store.LoadAsync(settingsPath, @"C:\Users\TestUser\Desktop", AppGameProfile.DarkSouls3);

        Assert.Equal("DarkSouls3", settings.GameId);
    }

    [Fact]
    public async Task LoadingOldSettingsWithoutOverlayBackgroundOpacityAppliesDefault()
    {
        var folder = Path.Combine(Path.GetTempPath(), "EldenDeathCounterTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        var settingsPath = Path.Combine(folder, "appsettings.json");
        await File.WriteAllTextAsync(settingsPath, """{"dataFolderPath":"C:\\Temp\\Counter","detectionPhrases":["YOU DIED"],"bossVictoryPhrases":["ENEMY FELLED"]}""");
        var store = new AppSettingsStore(new FileLogService(Path.Combine(folder, "log.txt")));

        var settings = await store.LoadAsync(settingsPath, @"C:\Users\TestUser\Desktop");

        Assert.Equal(0.9, settings.OverlayBackgroundOpacity);
    }

    [Fact]
    public async Task SavedSettingsDoNotPersistHardcodedDetectionPhrases()
    {
        var folder = Path.Combine(Path.GetTempPath(), "EldenDeathCounterTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        var settingsPath = Path.Combine(folder, "appsettings.json");
        var store = new AppSettingsStore(new FileLogService(Path.Combine(folder, "log.txt")));
        var settings = AppSettings.CreateDefault(@"C:\Users\TestUser\Desktop");

        await store.SaveAsync(settingsPath, settings);
        var json = await File.ReadAllTextAsync(settingsPath);

        Assert.DoesNotContain("detectionPhrases", json);
        Assert.DoesNotContain("bossVictoryPhrases", json);
    }

    [Fact]
    public async Task LoadingOldSettingsIgnoresPersistedDetectionPhrases()
    {
        var folder = Path.Combine(Path.GetTempPath(), "EldenDeathCounterTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        var settingsPath = Path.Combine(folder, "appsettings.json");
        await File.WriteAllTextAsync(settingsPath, """{"dataFolderPath":"C:\\Temp\\Counter","detectionPhrases":["CUSTOM"],"bossVictoryPhrases":["CUSTOM WIN"]}""");
        var store = new AppSettingsStore(new FileLogService(Path.Combine(folder, "log.txt")));

        var settings = await store.LoadAsync(settingsPath, @"C:\Users\TestUser\Desktop");

        Assert.Contains("YOU DIED", settings.DetectionPhrases);
        Assert.Contains("ENEMY FELLED", settings.BossVictoryPhrases);
        Assert.DoesNotContain("CUSTOM", settings.DetectionPhrases);
        Assert.DoesNotContain("CUSTOM WIN", settings.BossVictoryPhrases);
    }

    [Fact]
    public async Task SavedOverlayBackgroundOpacitySurvivesRoundTrip()
    {
        var folder = Path.Combine(Path.GetTempPath(), "EldenDeathCounterTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        var settingsPath = Path.Combine(folder, "appsettings.json");
        var store = new AppSettingsStore(new FileLogService(Path.Combine(folder, "log.txt")));
        var settings = AppSettings.CreateDefault(@"C:\Users\TestUser\Desktop");
        settings.OverlayBackgroundOpacity = 0.4;

        await store.SaveAsync(settingsPath, settings);
        var reloaded = await store.LoadAsync(settingsPath, @"C:\Users\TestUser\Desktop");

        Assert.Equal(0.4, reloaded.OverlayBackgroundOpacity);
    }

    [Fact]
    public async Task SavedBossHealthBarStyleSurvivesRoundTrip()
    {
        var folder = Path.Combine(Path.GetTempPath(), "EldenDeathCounterTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        var settingsPath = Path.Combine(folder, "appsettings.json");
        var store = new AppSettingsStore(new FileLogService(Path.Combine(folder, "log.txt")));
        var settings = AppSettings.CreateDefault(@"C:\Users\TestUser\Desktop");
        settings.BossHealthBarStyle = BossHealthBarStyles.Reforged;

        await store.SaveAsync(settingsPath, settings);
        var reloaded = await store.LoadAsync(settingsPath, @"C:\Users\TestUser\Desktop");

        Assert.Equal(BossHealthBarStyles.Reforged, reloaded.BossHealthBarStyle);
    }

    [Fact]
    public async Task LoadingSettingsWithoutOverlayToggleHotkeyAppliesDefault()
    {
        var folder = Path.Combine(Path.GetTempPath(), "EldenDeathCounterTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        var settingsPath = Path.Combine(folder, "appsettings.json");
        await File.WriteAllTextAsync(settingsPath, """{"dataFolderPath":"C:\\Temp\\Counter","detectionPhrases":["YOU DIED"],"bossVictoryPhrases":["ENEMY FELLED"]}""");
        var store = new AppSettingsStore(new FileLogService(Path.Combine(folder, "log.txt")));

        var settings = await store.LoadAsync(settingsPath, @"C:\Users\TestUser\Desktop");

        Assert.Equal("Ctrl+Shift+O", settings.OverlayToggleHotkey);
    }

    [Fact]
    public async Task LoadingOldDefaultManualHotkeysSwapsAddAndSubtract()
    {
        var folder = Path.Combine(Path.GetTempPath(), "EldenDeathCounterTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        var settingsPath = Path.Combine(folder, "appsettings.json");
        await File.WriteAllTextAsync(settingsPath, """{"dataFolderPath":"C:\\Temp\\Counter","manualAddHotkey":"F8","manualSubtractHotkey":"F9"}""");
        var store = new AppSettingsStore(new FileLogService(Path.Combine(folder, "log.txt")));

        var settings = await store.LoadAsync(settingsPath, @"C:\Users\TestUser\Desktop");

        Assert.Equal("F9", settings.ManualAddHotkey);
        Assert.Equal("F8", settings.ManualSubtractHotkey);
    }

    [Fact]
    public async Task LoadingOldSettingsAddsDetectionAndBossSkipHotkeyDefaults()
    {
        var folder = Path.Combine(Path.GetTempPath(), "EldenDeathCounterTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        var settingsPath = Path.Combine(folder, "appsettings.json");
        await File.WriteAllTextAsync(settingsPath, """{"dataFolderPath":"C:\\Temp\\Counter"}""");
        var store = new AppSettingsStore(new FileLogService(Path.Combine(folder, "log.txt")));

        var settings = await store.LoadAsync(settingsPath, @"C:\Users\TestUser\Desktop");

        Assert.Equal("F6", settings.DetectionToggleHotkey);
        Assert.Equal("Ctrl+Shift+P", settings.BossSkipHotkey);
    }

    [Fact]
    public async Task LoadingOldFastDetectionIntervalRaisesItToCurrentMinimum()
    {
        var folder = Path.Combine(Path.GetTempPath(), "EldenDeathCounterTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        var settingsPath = Path.Combine(folder, "appsettings.json");
        await File.WriteAllTextAsync(settingsPath, """{"dataFolderPath":"C:\\Temp\\Counter","detectionIntervalMs":200}""");
        var store = new AppSettingsStore(new FileLogService(Path.Combine(folder, "log.txt")));

        var settings = await store.LoadAsync(settingsPath, @"C:\Users\TestUser\Desktop");

        Assert.Equal(250, settings.DetectionIntervalMs);
    }

    [Fact]
    public async Task LoadingOldDetectionDefaultsMigratesIntervalAndCooldownTogether()
    {
        var folder = Path.Combine(Path.GetTempPath(), "EldenDeathCounterTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        var settingsPath = Path.Combine(folder, "appsettings.json");
        await File.WriteAllTextAsync(settingsPath, """{"dataFolderPath":"C:\\Temp\\Counter","detectionIntervalMs":500,"detectionCooldownSeconds":5}""");
        var store = new AppSettingsStore(new FileLogService(Path.Combine(folder, "log.txt")));

        var settings = await store.LoadAsync(settingsPath, @"C:\Users\TestUser\Desktop");

        Assert.Equal(350, settings.DetectionIntervalMs);
        Assert.Equal(25, settings.DetectionCooldownSeconds);
    }

    [Fact]
    public async Task LoadingOldSettingsAddsNewOverlayAndDetectionOptionDefaults()
    {
        var folder = Path.Combine(Path.GetTempPath(), "EldenDeathCounterTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        var settingsPath = Path.Combine(folder, "appsettings.json");
        await File.WriteAllTextAsync(settingsPath, """{"dataFolderPath":"C:\\Temp\\Counter"}""");
        var store = new AppSettingsStore(new FileLogService(Path.Combine(folder, "log.txt")));

        var settings = await store.LoadAsync(settingsPath, @"C:\Users\TestUser\Desktop");

        Assert.Equal("Balanced", settings.DetectionMode);
        Assert.True(settings.DetectDeaths);
        Assert.True(settings.DetectBossVictories);
        Assert.True(settings.ShowBossTimer);
        Assert.True(settings.ShowDetectionStatus);
    }

    [Fact]
    public void DetectionModePresetsExposeConservativeBalancedAndAggressiveValues()
    {
        Assert.Equal(new DetectionModePreset("Conservative", 500, 35, 0.9), DetectionModePresets.Get("Conservative"));
        Assert.Equal(new DetectionModePreset("Balanced", 350, 25, 0.8), DetectionModePresets.Get("Balanced"));
        Assert.Equal(new DetectionModePreset("Aggressive", 250, 10, 0.65), DetectionModePresets.Get("Aggressive"));
        Assert.Equal(DetectionModePresets.Get("Balanced"), DetectionModePresets.Get("unknown"));
    }

    [Fact]
    public async Task LoadingSettingsWithoutCharacterProfileNameAppliesEmptyDefault()
    {
        var folder = Path.Combine(Path.GetTempPath(), "EldenDeathCounterTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        var settingsPath = Path.Combine(folder, "appsettings.json");
        await File.WriteAllTextAsync(settingsPath, """{"dataFolderPath":"C:\\Temp\\Counter"}""");
        var store = new AppSettingsStore(new FileLogService(Path.Combine(folder, "log.txt")));

        var settings = await store.LoadAsync(settingsPath, @"C:\Users\TestUser\Desktop");

        Assert.Equal(string.Empty, settings.CharacterProfileName);
    }

    [Fact]
    public async Task SavedCharacterProfileNameSurvivesRoundTrip()
    {
        var folder = Path.Combine(Path.GetTempPath(), "EldenDeathCounterTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        var settingsPath = Path.Combine(folder, "appsettings.json");
        var store = new AppSettingsStore(new FileLogService(Path.Combine(folder, "log.txt")));
        var settings = AppSettings.CreateDefault(@"C:\Users\TestUser\Desktop");
        settings.CharacterProfileName = "Kamil";

        await store.SaveAsync(settingsPath, settings);
        var reloaded = await store.LoadAsync(settingsPath, @"C:\Users\TestUser\Desktop");

        Assert.Equal("Kamil", reloaded.CharacterProfileName);
    }

    [Fact]
    public async Task SavedOverlayToggleHotkeySurvivesRoundTrip()
    {
        var folder = Path.Combine(Path.GetTempPath(), "EldenDeathCounterTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        var settingsPath = Path.Combine(folder, "appsettings.json");
        var store = new AppSettingsStore(new FileLogService(Path.Combine(folder, "log.txt")));
        var settings = AppSettings.CreateDefault(@"C:\Users\TestUser\Desktop");
        settings.OverlayToggleHotkey = "Alt+F10";

        await store.SaveAsync(settingsPath, settings);
        var reloaded = await store.LoadAsync(settingsPath, @"C:\Users\TestUser\Desktop");

        Assert.Equal("Alt+F10", reloaded.OverlayToggleHotkey);
    }
    [Fact]
    public async Task LoadingOldSettingsAddsDiagnosticsDefaults()
    {
        var folder = Path.Combine(Path.GetTempPath(), "EldenDeathCounterTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        var settingsPath = Path.Combine(folder, "appsettings.json");
        await File.WriteAllTextAsync(settingsPath, """{"dataFolderPath":"C:\\Temp\\Counter","detectionPhrases":["YOU DIED"],"bossVictoryPhrases":["ENEMY FELLED"]}""");
        var store = new AppSettingsStore(new FileLogService(Path.Combine(folder, "log.txt")));

        var settings = await store.LoadAsync(settingsPath, @"C:\Users\TestUser\Desktop");

        Assert.Equal(DiagnosticsMode.Events, settings.DiagnosticsMode);
        Assert.Equal(10, settings.DiagnosticsSessionMinutes);
        Assert.Equal(5, settings.DiagnosticsMaxEventLogMb);
        Assert.Equal(7, settings.DiagnosticsRetentionDays);
    }
}
