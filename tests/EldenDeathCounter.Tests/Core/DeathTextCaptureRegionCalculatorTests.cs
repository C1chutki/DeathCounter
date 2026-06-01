using EldenDeathCounter.Core.Detection;

namespace EldenDeathCounter.Tests.Core;

public sealed class DeathTextCaptureRegionCalculatorTests
{
    // Measured from real in-game captures at 2560x1440: "YOU DIED" and boss-victory
    // ("ENEMY FELLED"/"POKONANO WROGA") text occupy a vertical band of roughly y 693..797.
    private const int TextBandTop = 693;
    private const int TextBandBottom = 797;

    [Fact]
    public void CoversKnownDeathTextBandOn1440pScreenshots()
    {
        var region = DeathTextCaptureRegionCalculator.Calculate(2560, 1440);

        Assert.True(region.Left <= 650, $"left={region.Left}");
        Assert.True(region.Right >= 1920, $"right={region.Right}");
        Assert.True(region.Top <= TextBandTop, $"top={region.Top}");
        Assert.True(region.Bottom >= TextBandBottom, $"bottom={region.Bottom}");
        Assert.True(region.Width < 1800, $"width={region.Width}");
        Assert.True(region.Height < 260, $"height={region.Height}");
    }

    [Fact]
    public void TightensVerticalBandWhileKeepingSafetyMarginOn1440p()
    {
        var region = DeathTextCaptureRegionCalculator.Calculate(2560, 1440);

        // Tighter band than the previous 0.26 fraction (374px): ~80px trimmed top and bottom,
        // leaving a band around 216px tall centered on the measured text while keeping enough
        // vertical slide room for template matching.
        Assert.True(region.Top > 504, $"top={region.Top}");
        Assert.True(region.Height < 240, $"height={region.Height}");

        // Still keeps at least ~25px of safety margin around the measured 693..797 text band.
        Assert.True(region.Top <= TextBandTop - 25, $"top={region.Top}");
        Assert.True(region.Bottom >= TextBandBottom + 25, $"bottom={region.Bottom}");
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
        Assert.True(region.Height >= 160);
    }
}
