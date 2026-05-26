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
        Assert.Equal("Strażnik Drzewa", settings.BossNameCorrections["STRAINIK DRZEWA"]);
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
