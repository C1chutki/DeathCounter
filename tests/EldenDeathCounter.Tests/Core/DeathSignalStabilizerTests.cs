using EldenDeathCounter.Core.Detection;

namespace EldenDeathCounter.Tests.Core;

public sealed class DeathSignalStabilizerTests
{
    [Fact]
    public void RequiresConsecutiveSameSignalBeforeConfirming()
    {
        var stabilizer = new DeathSignalStabilizer(requiredConsecutiveFrames: 2);

        var first = new ImageDeathSignalMatch(true, 0.91, "template:NIE ZYJESZ", 1.0);
        var second = new ImageDeathSignalMatch(true, 0.93, "template:NIE ZYJESZ", 1.0);

        Assert.Null(stabilizer.Observe(first));
        var confirmed = stabilizer.Observe(second);

        Assert.NotNull(confirmed);
        Assert.Equal("template:NIE ZYJESZ", confirmed.Method);
        Assert.Equal(0.93, confirmed.Score, precision: 2);
    }

    [Fact]
    public void ConfirmsIntermittentSignalDuringFadeAnimation()
    {
        var stabilizer = new DeathSignalStabilizer(requiredConsecutiveFrames: 2);
        var signal = new ImageDeathSignalMatch(true, 0.91, "ocr:NIE ZYJESZ", 1.0);

        Assert.Null(stabilizer.Observe(signal));
        Assert.Null(stabilizer.Observe(ImageDeathSignalMatch.NoMatch));
        var confirmed = stabilizer.Observe(signal);

        Assert.NotNull(confirmed);
        Assert.Equal("ocr:NIE ZYJESZ", confirmed.Method);
    }

    [Fact]
    public void ConfirmsWhenOcrAndTemplateAlternateAcrossFrames()
    {
        var stabilizer = new DeathSignalStabilizer(requiredConsecutiveFrames: 2);

        Assert.Null(stabilizer.Observe(new ImageDeathSignalMatch(true, 1, "ocr:NIE ZYJESZ", 1.0)));
        var confirmed = stabilizer.Observe(new ImageDeathSignalMatch(true, 0.52, "template:NIE ZYJESZ", 0.95));

        Assert.NotNull(confirmed);
        Assert.Equal("template:NIE ZYJESZ", confirmed.Method);
    }

    [Fact]
    public void ConfirmsPendingSignalWithNearThresholdTemplateCandidate()
    {
        var stabilizer = new DeathSignalStabilizer(requiredConsecutiveFrames: 2);
        var first = new ImageDeathSignalMatch(true, 0.359, "template:Death_Screen.png", 1.0);
        var nearThreshold = new ImageDeathSignalMatch(false, 0.282, "template:Death_Screen.png", 1.0)
        {
            Details = "threshold=0.288; candidate=900,600,700x120; contrast=True; strokeRatio=0.610; backgroundRatio=0.070; verticalCoverage=0.720"
        };

        Assert.Null(stabilizer.Observe(first));
        var confirmed = stabilizer.Observe(nearThreshold);

        Assert.NotNull(confirmed);
        Assert.True(confirmed.IsMatch);
        Assert.Equal("template:Death_Screen.png", confirmed.Method);
        Assert.Equal(0.282, confirmed.Score, precision: 3);
    }

    [Fact]
    public void DoesNotStartPendingWithNearThresholdTemplateCandidate()
    {
        var stabilizer = new DeathSignalStabilizer(requiredConsecutiveFrames: 2);
        var nearThreshold = new ImageDeathSignalMatch(false, 0.282, "template:Death_Screen.png", 1.0)
        {
            Details = "threshold=0.288; candidate=900,600,700x120; contrast=True; strokeRatio=0.610; backgroundRatio=0.070; verticalCoverage=0.720"
        };

        Assert.Null(stabilizer.Observe(nearThreshold));
        Assert.False(stabilizer.IsPending);
    }
}
