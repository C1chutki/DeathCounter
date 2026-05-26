using System.Drawing;
using EldenDeathCounter.Core.Detection;

namespace EldenDeathCounter.Tests.Core;

public sealed class BossHealthBarAnalyzerTests
{
    [Fact]
    public void FindsSingleLongBossHealthBarInBottomRegion()
    {
        var analyzer = new BossHealthBarAnalyzer();

        var bars = analyzer.Analyze(1920, 1080, (x, y) =>
        {
            var bossBar = x is > 470 and < 1510 && y is > 900 and < 916;
            return bossBar ? new RgbPixel(130, 18, 22) : new RgbPixel(70, 76, 55);
        });

        var bar = Assert.Single(bars);
        Assert.InRange(bar.Bar.Left, 460, 490);
        Assert.InRange(bar.Bar.Right, 1490, 1520);
        Assert.True(bar.NameRegion.Top < bar.Bar.Top);
        Assert.Equal(bar.Bar.Left, bar.NameRegion.Left);
    }

    [Fact]
    public void FindsTwoStackedBossHealthBars()
    {
        var analyzer = new BossHealthBarAnalyzer();

        var bars = analyzer.Analyze(1920, 1080, (x, y) =>
        {
            var first = x is > 430 and < 1450 && y is > 842 and < 856;
            var second = x is > 430 and < 1450 && y is > 910 and < 924;
            return first || second ? new RgbPixel(130, 18, 22) : new RgbPixel(70, 76, 55);
        });

        Assert.Equal(2, bars.Count);
        Assert.True(bars[0].Bar.Top < bars[1].Bar.Top);
    }

    [Fact]
    public void FindsBossHealthBarInLowerScreenCrop()
    {
        var analyzer = new BossHealthBarAnalyzer();

        var bars = analyzer.Analyze(1920, 340, (x, y) =>
        {
            var bossBar = x is > 470 and < 1510 && y is > 135 and < 151;
            return bossBar ? new RgbPixel(130, 18, 22) : new RgbPixel(70, 76, 55);
        });

        var bar = Assert.Single(bars);
        Assert.InRange(bar.Bar.Left, 460, 490);
        Assert.InRange(bar.Bar.Top, 130, 140);
        Assert.True(bar.NameRegion.Top < bar.Bar.Top);
    }

    [Theory]
    [InlineData("ENG_Boss_bar.jpg")]
    [InlineData("ENG_Boss_bar_v2.jpg")]
    public void FindsEnglishBossHealthBarWhenCurrentHealthIsBelowMinimumBossBarWidth(string assetName)
    {
        var analyzer = new BossHealthBarAnalyzer();
        using var bitmap = new Bitmap(GetAssetPath(assetName));

        var bars = analyzer.Analyze(bitmap.Width, bitmap.Height, (x, y) =>
        {
            var pixel = bitmap.GetPixel(x, y);
            return new RgbPixel(pixel.R, pixel.G, pixel.B);
        });

        var bar = Assert.Single(bars);
        Assert.InRange(bar.Bar.Left, 600, 650);
        Assert.InRange(bar.Bar.Top, 1150, 1170);
        Assert.True(bar.Bar.Right - bar.Bar.Left >= bitmap.Width * 0.32);
    }

    [Fact]
    public void IgnoresShortEnemyHealthBarAndTopHudBars()
    {
        var analyzer = new BossHealthBarAnalyzer();

        var bars = analyzer.Analyze(1920, 1080, (x, y) =>
        {
            var topHud = x is > 120 and < 450 && y is > 55 and < 75;
            var shortEnemyBar = x is > 1010 and < 1180 && y is > 180 and < 190;
            return topHud || shortEnemyBar ? new RgbPixel(130, 18, 22) : new RgbPixel(70, 76, 55);
        });

        Assert.Empty(bars);
    }

    private static string GetAssetPath(string assetName)
    {
        return Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..",
            "..",
            "..",
            "..",
            "..",
            "Assets",
            assetName));
    }
}
