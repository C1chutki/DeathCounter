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

    [Fact]
    public void FindsSekiroTopLeftBossHealthBarWithNameRegionBelowTheBar()
    {
        // Sekiro's bar is a short crimson strip at the top-left (x ~0.06..0.28 of the screen) with the
        // name plate *below* it, so the scan window, the span thresholds and the name band all differ
        // from every bottom-centre game. The capture handed to the analyzer is the upper band
        // (y 0.02..0.22 of a 2560x1440 screen), i.e. 2560x288 with the bar at crop y ~91..106.
        var analyzer = new BossHealthBarAnalyzer();

        var bars = analyzer.Analyze(2560, 288, "Sekiro", (x, y) =>
        {
            var bossBar = x is > 150 and < 715 && y is > 91 and < 106;
            return bossBar ? new RgbPixel(150, 34, 30) : new RgbPixel(60, 66, 72);
        });

        var bar = Assert.Single(bars);
        Assert.InRange(bar.Bar.Left, 130, 165);
        Assert.InRange(bar.Bar.Right, 700, 740);

        // The name "Guardian Ape" sits at crop y ~136..161, under the bar.
        Assert.True(bar.NameRegion.Top > bar.Bar.Bottom - 8, "Sekiro's name region must sit below the bar.");
        Assert.True(bar.NameRegion.Top <= 136 && bar.NameRegion.Bottom >= 161, "Name region must cover the name plate.");
    }

    [Fact]
    public void EldenRingTuningMissesTheSekiroBarBecauseItScansTheBottomOfAFullFrame()
    {
        // Same top-left bar, but on a full frame with the default tuning: nothing is found, which is why
        // Sekiro needs both its own capture band and its own scan window.
        var analyzer = new BossHealthBarAnalyzer();

        var bars = analyzer.Analyze(2560, 1440, (x, y) =>
        {
            var bossBar = x is > 150 and < 715 && y is > 120 and < 135;
            return bossBar ? new RgbPixel(150, 34, 30) : new RgbPixel(60, 66, 72);
        });

        Assert.Empty(bars);
    }

    [Theory]
    [InlineData("ENG_Boss_bar.jpg")]
    [InlineData("ENG_Boss_bar_v2.jpg")]
    public void FindsEnglishBossHealthBarWhenCurrentHealthIsBelowMinimumBossBarWidth(string assetName)
    {
        var analyzer = new BossHealthBarAnalyzer();
        using var bitmap = new Bitmap(GetAssetPath(Path.Combine("Elden Ring", assetName)));

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

    [Fact]
    public void FindsDarkSouls3DimBossHealthBarWithNameRegionCoveringTheBossName()
    {
        // Dark Souls III's bar is a dim, thin crimson line (red channel ~48-72, vs Elden Ring's ~130)
        // and its boss name sits left of the visible red fill. With the DS3 tuning the bar is found and
        // the name region reaches the "Iudex Gundyr" text (x ~ 620..940) so the name OCR can read it.
        var analyzer = new BossHealthBarAnalyzer();
        using var bitmap = new Bitmap(GetAssetPath(Path.Combine("Dark souls 3", "PL_BossBar.jpg")));

        var bars = analyzer.Analyze(bitmap.Width, bitmap.Height, "DarkSouls3", (x, y) =>
        {
            var pixel = bitmap.GetPixel(x, y);
            return new RgbPixel(pixel.R, pixel.G, pixel.B);
        });

        var bar = Assert.Single(bars);
        Assert.InRange(bar.Bar.Top, 1190, 1216);
        Assert.True(bar.NameRegion.Top < bar.Bar.Top);
        Assert.True(
            bar.NameRegion.Left <= 640,
            $"Name region left {bar.NameRegion.Left} should reach the boss name start (~620).");
        Assert.True(
            bar.NameRegion.Right >= 920,
            $"Name region right {bar.NameRegion.Right} should cover the boss name end (~940).");
    }

    [Fact]
    public void DarkSouls3TuningDoesNotDetectTheDimBarAsEldenRing()
    {
        // Guard the appearance fix: the same dim DS3 frame must NOT match under Elden Ring's brighter
        // red floor in a way that mislocates the name region, confirming the tuning is game-specific.
        var analyzer = new BossHealthBarAnalyzer();
        using var bitmap = new Bitmap(GetAssetPath(Path.Combine("Dark souls 3", "PL_BossBar.jpg")));

        RgbPixel Sample(int x, int y)
        {
            var pixel = bitmap.GetPixel(x, y);
            return new RgbPixel(pixel.R, pixel.G, pixel.B);
        }

        var elden = analyzer.Analyze(bitmap.Width, bitmap.Height, "EldenRing", Sample);
        var ds3 = analyzer.Analyze(bitmap.Width, bitmap.Height, "DarkSouls3", Sample);

        // Under Elden Ring tuning the name region anchors to the red fill and misses the name; the DS3
        // tuning shifts it left so it covers the name.
        Assert.True(ds3.Single().NameRegion.Left < elden.Single().NameRegion.Left);
    }

    [Fact]
    public void FindsReforgedBossHealthBarFromReferenceCapture()
    {
        var analyzer = new BossHealthBarAnalyzer();
        using var bitmap = new Bitmap(GetAssetPath(Path.Combine("Elden Ring", "Reforge", "BossBar_Reforge.png")));

        var bars = analyzer.Analyze(bitmap.Width, bitmap.Height, "EldenRing", BossHealthBarStyles.Reforged, (x, y) =>
        {
            var pixel = bitmap.GetPixel(x, y);
            return new RgbPixel(pixel.R, pixel.G, pixel.B);
        });

        var bar = Assert.Single(bars);
        Assert.InRange(bar.Bar.Left, 600, 680);
        Assert.InRange(bar.Bar.Top, 1250, 1305);
        Assert.True(bar.Bar.Right - bar.Bar.Left >= bitmap.Width * 0.32);
        Assert.True(bar.NameRegion.Top < bar.Bar.Top);
    }

    [Fact]
    public void ReforgedTuningIgnoresTallRedSceneryBlobsAboveTheBossBar()
    {
        var analyzer = new BossHealthBarAnalyzer();

        var bars = analyzer.Analyze(2559, 1439, "EldenRing", BossHealthBarStyles.Reforged, (x, y) =>
        {
            var bloodPool = x is > 1600 and < 2200 && y is > 1133 and < 1199;
            var bossBar = x is > 620 and < 1940 && y is > 1263 and < 1277;
            return bloodPool || bossBar ? new RgbPixel(120, 8, 8) : new RgbPixel(54, 48, 38);
        });

        var bar = Assert.Single(bars);
        Assert.InRange(bar.Bar.Left, 600, 640);
        Assert.InRange(bar.Bar.Top, 1255, 1270);
        Assert.InRange(bar.Bar.Right, 1930, 1955);
    }

    [Fact]
    public void FindsDarkSouls2DimBossHealthBarWithNameRegionCoveringTheLastGiant()
    {
        // Dark Souls II's boss bar is a dark, desaturated crimson (avg ~R50,G28,B24) sitting near the
        // bottom of the screen (y ~1225..1243 at 1440p). With the DS2 tuning it is found as a single bar
        // and the name region reaches the "The Last Giant" text (starts at x ~718) so the name OCR can
        // read it. The two summon bars and the top-left bar sit above the y0.70 scan floor and are
        // excluded.
        var analyzer = new BossHealthBarAnalyzer();
        using var bitmap = new Bitmap(GetAssetPath(Path.Combine("Dark souls 2", "ENG_BossBar.jpg")));

        var bars = analyzer.Analyze(bitmap.Width, bitmap.Height, "DarkSouls2", (x, y) =>
        {
            var pixel = bitmap.GetPixel(x, y);
            return new RgbPixel(pixel.R, pixel.G, pixel.B);
        });

        var bar = Assert.Single(bars);
        Assert.InRange(bar.Bar.Top, 1210, 1240);
        Assert.InRange(bar.Bar.Bottom, 1235, 1260);
        Assert.True(bar.Bar.Right - bar.Bar.Left >= bitmap.Width * 0.32);
        Assert.True(bar.NameRegion.Top < bar.Bar.Top);
        Assert.True(
            bar.NameRegion.Left <= 720,
            $"Name region left {bar.NameRegion.Left} should reach the boss name start (~718).");
        Assert.True(
            bar.NameRegion.Right >= 1040,
            $"Name region right {bar.NameRegion.Right} should cover the boss name end (~1037).");
    }

    [Fact]
    public void DarkSouls2TuningIsRequiredBecauseEldenRingTuningMissesTheDimBar()
    {
        // Guard that the DS2 tuning is genuinely needed: Elden Ring's brighter red floor and tighter
        // colour ceilings reject the dark DS2 bar entirely, so the same frame yields no bar as Elden
        // Ring but exactly one under DS2.
        var analyzer = new BossHealthBarAnalyzer();
        using var bitmap = new Bitmap(GetAssetPath(Path.Combine("Dark souls 2", "ENG_BossBar.jpg")));

        RgbPixel Sample(int x, int y)
        {
            var pixel = bitmap.GetPixel(x, y);
            return new RgbPixel(pixel.R, pixel.G, pixel.B);
        }

        var elden = analyzer.Analyze(bitmap.Width, bitmap.Height, "EldenRing", Sample);
        var ds2 = analyzer.Analyze(bitmap.Width, bitmap.Height, "DarkSouls2", Sample);

        Assert.Empty(elden);
        Assert.Single(ds2);
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
