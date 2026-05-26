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
