using EldenDeathCounter.Core.Configuration;
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
        Assert.Equal(300, settings.DetectionIntervalMs);
        Assert.Equal(25, settings.DetectionCooldownSeconds);
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
        Assert.Equal("F8", settings.ManualAddHotkey);
        Assert.Equal("F9", settings.ManualSubtractHotkey);
        Assert.Equal("F7", settings.BossDefeatedHotkey);
        Assert.Equal("Ctrl+Shift+O", settings.OverlayToggleHotkey);
        Assert.Equal("F6", settings.DetectionToggleHotkey);
        Assert.Equal("Ctrl+Shift+P", settings.BossSkipHotkey);
        Assert.Equal(1.0, settings.OverlayFontScale);
        Assert.Equal(0.9, settings.OverlayBackgroundOpacity);
        Assert.Equal("Strażnik Drzewa", settings.BossNameCorrections["STRAINIK DRZEWA"]);
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
