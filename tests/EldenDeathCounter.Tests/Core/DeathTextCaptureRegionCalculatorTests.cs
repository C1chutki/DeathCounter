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
    public void DarkSouls2CoversLowerDeathTextBandThatEldenRingBandMisses()
    {
        // DS2 draws "YOU DIED" much lower (~y 0.66..0.74 -> ~947..1069 at 1440p). The DS2 band must
        // cover it, and the default (Elden Ring) band must NOT — proving the game-aware ROI is needed.
        const int ds2TextTop = 947;
        const int ds2TextBottom = 1069;

        var ds2 = DeathTextCaptureRegionCalculator.Calculate(2560, 1440, "DarkSouls2");
        var elden = DeathTextCaptureRegionCalculator.Calculate(2560, 1440, "EldenRing");

        Assert.True(ds2.Top <= ds2TextTop, $"ds2.Top={ds2.Top}");
        Assert.True(ds2.Bottom >= ds2TextBottom, $"ds2.Bottom={ds2.Bottom}");
        Assert.True(elden.Bottom < ds2TextTop, $"elden.Bottom={elden.Bottom} should not reach the DS2 band");
        Assert.True(ds2.Top > elden.Bottom, "DS2 band should sit below the Elden Ring band");
    }

    [Fact]
    public void SekiroBandFitsTheDeathKanjiInsideTheTemplateSearchWindow()
    {
        // Measured on a 1920x1080 Sekiro death screen: the 死 kanji spans y ~330..610 and the spaced
        // "D E A T H" sits at ~645..665. The template matcher only searches the middle 18%..78% of the
        // ROI, so the kanji has to fall inside that sub-band, and OCR still needs the Latin text.
        const int kanjiTop = 330;
        const int kanjiBottom = 610;
        const int latinTextBottom = 665;

        var region = DeathTextCaptureRegionCalculator.Calculate(1920, 1080, "Sekiro");
        var searchTop = region.Top + (int)(region.Height * 0.18);
        var searchBottom = region.Top + (int)(region.Height * 0.78);

        Assert.True(searchTop <= kanjiTop, $"searchTop={searchTop}");
        Assert.True(searchBottom >= kanjiBottom, $"searchBottom={searchBottom}");
        Assert.True(region.Bottom >= latinTextBottom, $"bottom={region.Bottom}");
        Assert.True(region.Top >= 0 && region.Bottom <= 1080);
    }

    [Fact]
    public void DefaultOverloadMatchesEldenRingGameAwareOverload()
    {
        var legacy = DeathTextCaptureRegionCalculator.Calculate(2560, 1440);
        var elden = DeathTextCaptureRegionCalculator.Calculate(2560, 1440, "EldenRing");

        Assert.Equal(legacy, elden);
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
