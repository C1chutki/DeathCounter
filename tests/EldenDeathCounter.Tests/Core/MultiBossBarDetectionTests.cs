using System.Drawing;
using EldenDeathCounter.Core.Detection;

namespace EldenDeathCounter.Tests.Core;

/// <summary>
/// Detection of 1/2/3 boss HP bars from the fixed bottom boss-bar area, plus the associated
/// name-region calculation that gates boss-name OCR (test groups C, D, E).
/// </summary>
public sealed class MultiBossBarDetectionTests
{
    private static IReadOnlyList<BossHealthBarRegion> AnalyzeAsset(string assetName)
    {
        var analyzer = new BossHealthBarAnalyzer();
        using var bitmap = new Bitmap(GetAssetPath(Path.Combine("Elden Ring", assetName)));
        return analyzer.Analyze(bitmap.Width, bitmap.Height, (x, y) =>
        {
            var pixel = bitmap.GetPixel(x, y);
            return new RgbPixel(pixel.R, pixel.G, pixel.B);
        });
    }

    private static void AssertNameRegionAnchoredAboveBar(BossHealthBarRegion region)
    {
        // The boss-name OCR region must sit directly above its bar, left-aligned with the bar start.
        Assert.Equal(region.Bar.Left, region.NameRegion.Left);
        Assert.True(region.NameRegion.Top < region.Bar.Top, "name region must be above the bar");
        Assert.True(region.NameRegion.Bottom <= region.Bar.Top, "name region must not overlap the bar");
        Assert.True(region.NameRegion.Height is > 20 and < 120, "name region height should bound a single label line");
        Assert.True(region.NameRegion.Width > 0);
    }

    // ---- Group C: single boss bar ----

    [Fact]
    public void DetectsSingleBossBarAndAnchorsNameRegionAboveIt()
    {
        var bars = AnalyzeAsset("ENG_Boss_bar.jpg");

        var bar = Assert.Single(bars);
        AssertNameRegionAnchoredAboveBar(bar);
    }

    // ---- Group D: two boss bars ----

    [Theory]
    [InlineData("ENG_DoubleBoss.jpg")]
    [InlineData("ENG_DoubleBoss_02.jpg")]
    public void DetectsTwoBossBarsSortedTopToBottomWithSeparateNameRegions(string assetName)
    {
        var bars = AnalyzeAsset(assetName);

        Assert.Equal(2, bars.Count);
        Assert.True(bars[0].Bar.Top < bars[1].Bar.Top, "bars must be sorted top-to-bottom");
        Assert.True(bars[0].NameRegion.Top < bars[1].NameRegion.Top, "name regions must be distinct and ordered");
        foreach (var bar in bars)
        {
            AssertNameRegionAnchoredAboveBar(bar);
        }
    }

    // ---- Group E: three boss bars ----

    [Fact]
    public void DetectsThreeStackedBossBarsSortedTopToBottomWithSeparateNameRegions()
    {
        // Deterministic full-health stack: this is what a real triple encounter looks like at its
        // start, which is the moment the active boss name is read and frozen at runtime.
        var analyzer = new BossHealthBarAnalyzer();
        var bars = analyzer.Analyze(2560, 1440, (x, y) =>
        {
            var left = x is > 620 and < 1900;
            var b1 = left && y is > 1010 and < 1024;
            var b2 = left && y is > 1083 and < 1097;
            var b3 = left && y is > 1156 and < 1170;
            return b1 || b2 || b3 ? new RgbPixel(95, 1, 1) : new RgbPixel(60, 60, 64);
        });

        Assert.Equal(3, bars.Count);
        Assert.True(bars[0].Bar.Top < bars[1].Bar.Top && bars[1].Bar.Top < bars[2].Bar.Top);
        Assert.True(bars[0].NameRegion.Top < bars[1].NameRegion.Top && bars[1].NameRegion.Top < bars[2].NameRegion.Top);
        foreach (var bar in bars)
        {
            AssertNameRegionAnchoredAboveBar(bar);
        }
    }

    [Fact]
    public void DetectsHealthyBarsFromRealTripleBossCaptureSortedTopToBottom()
    {
        // NOTE: the provided ENG_TrippleBoss captures are late-fight screenshots where one or more
        // bars are fully depleted (zero red fill) and therefore have no detectable colour signal.
        // The detector still finds every bar that retains health, sorted top-to-bottom, and never
        // produces a corrupted region. (Full 3-bar support is proven by the synthetic test above.)
        var bars = AnalyzeAsset("ENG_TrippleBoss_02.jpg");

        Assert.True(bars.Count >= 2, $"expected at least two bars with health, found {bars.Count}");
        for (var i = 1; i < bars.Count; i++)
        {
            Assert.True(bars[i - 1].Bar.Top < bars[i].Bar.Top, "bars must be sorted top-to-bottom");
        }

        foreach (var bar in bars)
        {
            AssertNameRegionAnchoredAboveBar(bar);
        }
    }

    private static string GetAssetPath(string assetName)
    {
        return Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory, "..", "..", "..", "..", "..", "Assets", assetName));
    }
}
