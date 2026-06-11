using EldenDeathCounter.Core.Detection;

namespace EldenDeathCounter.Tests.Core;

public sealed class DetectionTimingOptionsTests
{
    [Fact]
    public void NormalizesBaseIntervalToAtLeast250Milliseconds()
    {
        Assert.Equal(250, DetectionTimingOptions.NormalizeBaseIntervalMs(0));
        Assert.Equal(300, DetectionTimingOptions.NormalizeBaseIntervalMs(300));
        Assert.Equal(500, DetectionTimingOptions.NormalizeBaseIntervalMs(500));
    }

    [Fact]
    public void UsesBurstIntervalForWeakTemplateOrPendingSignal()
    {
        var weakTemplate = new ImageDeathSignalMatch(false, 0.35, "template:ENG_Death_Screen.png:opencv", 1.0);

        Assert.True(DetectionTimingOptions.ShouldEnterBurst(weakTemplate, false, ImageDeathSignalMatch.NoMatch));
        Assert.True(DetectionTimingOptions.ShouldEnterBurst(ImageDeathSignalMatch.NoMatch, true, ImageDeathSignalMatch.NoMatch));
        Assert.True(DetectionTimingOptions.ShouldEnterBurst(ImageDeathSignalMatch.NoMatch, false, weakTemplate));
        Assert.False(DetectionTimingOptions.ShouldEnterBurst(ImageDeathSignalMatch.NoMatch, false, ImageDeathSignalMatch.NoMatch));
    }

    [Fact]
    public void BurstConstantsUse200MillisecondsForShortFollowUpWindow()
    {
        Assert.Equal(200, DetectionTimingOptions.BurstIntervalMs);
        Assert.Equal(1500, DetectionTimingOptions.BurstDurationMs);
    }

    [Fact]
    public void FullDiagnosticsScreenshotSamplerSavesFirstFrameAndPeriodicFollowUps()
    {
        var first = new DateTimeOffset(2026, 6, 10, 16, 0, 0, TimeSpan.FromHours(2));

        Assert.True(DetectionTimingOptions.ShouldSaveFullDiagnosticsFrame(first, null, 1));
        Assert.False(DetectionTimingOptions.ShouldSaveFullDiagnosticsFrame(first.AddMilliseconds(900), first, 2));
        Assert.True(DetectionTimingOptions.ShouldSaveFullDiagnosticsFrame(first.AddSeconds(1), first, 3));
        Assert.True(DetectionTimingOptions.ShouldSaveFullDiagnosticsFrame(first.AddMilliseconds(200), first, 10));
    }
}
