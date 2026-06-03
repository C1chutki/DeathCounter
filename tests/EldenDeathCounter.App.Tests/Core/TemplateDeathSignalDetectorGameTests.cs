using System.Drawing;
using EldenDeathCounter.Core.Logging;
using EldenDeathCounter.Detection;

namespace EldenDeathCounter.App.Tests.Core;

public sealed class TemplateDeathSignalDetectorGameTests
{
    [Fact]
    public void DarkSouls3DetectorMatchesTheDarkSouls3DeathScreen()
    {
        var detector = new TemplateDeathTextImageSignalDetector(new SilentLog());
        using var deathScreen = LoadDarkSouls3DeathScreen();

        var result = detector.Analyze(deathScreen, 0.8, "DarkSouls3", "PL");

        Assert.True(
            result.IsMatch,
            $"score={result.Score:0.000}, method={result.Method}, details={result.Details}");
    }

    [Fact]
    public void EldenRingDetectorDoesNotMatchTheDarkSouls3DeathScreen()
    {
        // The Elden Ring death templates must not fire on a Dark Souls 3 frame: that would be the
        // cross-game false-positive the per-game template wiring exists to prevent.
        var detector = new TemplateDeathTextImageSignalDetector(new SilentLog());
        using var deathScreen = LoadDarkSouls3DeathScreen();

        var result = detector.Analyze(deathScreen, 0.8, "EldenRing", "PL");

        Assert.False(result.IsMatch, $"score={result.Score:0.000}, method={result.Method}");
    }

    private static Bitmap LoadDarkSouls3DeathScreen()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Assets", "Dark souls 3", "ENG_YouDied.jpg");
        Assert.True(File.Exists(path), $"Missing test asset: {path}");
        return new Bitmap(path);
    }

    private sealed class SilentLog : ILogService
    {
        public void Info(string message)
        {
        }

        public void Error(string message, Exception? exception = null)
        {
        }
    }
}
