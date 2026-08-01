using EldenDeathCounter.Core.Detection;

namespace EldenDeathCounter.Tests.Core;

public sealed class BossHealthBarCaptureRegionCalculatorTests
{
    [Fact]
    public void CapturesOnlyLowerScreenBandContainingBossBars()
    {
        var region = BossHealthBarCaptureRegionCalculator.Calculate(1920, 1080);

        Assert.Equal(0, region.Left);
        Assert.Equal(1920, region.Right);
        Assert.InRange(region.Top, 680, 700);
        Assert.InRange(region.Bottom, 1030, 1045);
        Assert.True(region.Height < 1080 / 2);
    }

    [Fact]
    public void SekiroCapturesUpperScreenBandBecauseItsBossBarIsAtTheTop()
    {
        var region = BossHealthBarCaptureRegionCalculator.Calculate(1920, 1080, "Sekiro");

        Assert.Equal(0, region.Left);
        Assert.Equal(1920, region.Right);
        Assert.InRange(region.Top, 15, 30);
        Assert.InRange(region.Bottom, 230, 245);
    }
}
