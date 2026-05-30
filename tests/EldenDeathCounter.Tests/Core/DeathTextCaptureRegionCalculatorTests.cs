using EldenDeathCounter.Core.Detection;

namespace EldenDeathCounter.Tests.Core;

public sealed class DeathTextCaptureRegionCalculatorTests
{
    [Fact]
    public void CoversKnownDeathTextBandOn1440pScreenshots()
    {
        var region = DeathTextCaptureRegionCalculator.Calculate(2560, 1440);

        Assert.True(region.Left <= 650, $"left={region.Left}");
        Assert.True(region.Right >= 1920, $"right={region.Right}");
        Assert.True(region.Top <= 619, $"top={region.Top}");
        Assert.True(region.Bottom >= 835, $"bottom={region.Bottom}");
        Assert.True(region.Width < 1800, $"width={region.Width}");
        Assert.True(region.Height < 520, $"height={region.Height}");
    }

    [Fact]
    public void TightensVerticalBandWhileKeepingSafetyMarginOn1440p()
    {
        var region = DeathTextCaptureRegionCalculator.Calculate(2560, 1440);

        // Tighter band than the previous 0.32 fraction: top sits lower (>504)
        // and the band is smaller (height 374 vs the previous 461, ~19% fewer rows).
        Assert.True(region.Top > 504, $"top={region.Top}");
        Assert.True(region.Height < 420, $"height={region.Height}");

        // Still keeps at least ~50px of safety margin around the known 619..835 text band.
        Assert.True(region.Top <= 619 - 50, $"top={region.Top}");
        Assert.True(region.Bottom >= 835 + 50, $"bottom={region.Bottom}");
    }

    [Fact]
    public void KeepsRegionInsideSmallScreens()
    {
        var region = DeathTextCaptureRegionCalculator.Calculate(1280, 720);

        Assert.True(region.Left >= 0);
        Assert.True(region.Top >= 0);
        Assert.True(region.Right <= 1280);
        Assert.True(region.Bottom <= 720);
        Assert.True(region.Width >= 640);
        Assert.True(region.Height >= 260);
    }
}
