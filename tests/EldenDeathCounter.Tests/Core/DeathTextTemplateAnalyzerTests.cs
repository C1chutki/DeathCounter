using EldenDeathCounter.Core.Detection;

namespace EldenDeathCounter.Tests.Core;

public sealed class DeathTextTemplateAnalyzerTests
{
    [Fact]
    public void MatchesDeathTextShapeOnDarkBackground()
    {
        var template = CreateTemplate();
        var analyzer = new DeathTextTemplateAnalyzer();

        var result = analyzer.Analyze(320, 160, (x, y) =>
        {
            var inText = IsSyntheticTextPixel(x - 112, y - 68);
            return inText ? new RgbPixel(130, 10, 22) : new RgbPixel(34, 34, 34);
        }, [template], 0.75);

        Assert.True(result.IsMatch, $"score={result.Score:0.000}, scale={result.Scale:0.00}, method={result.Method}");
        Assert.True(result.Score >= 0.50);
    }

    [Fact]
    public void MatchesVisibleDeathTextBelowOldStrictTemplateThreshold()
    {
        var template = CreateTemplate();
        var analyzer = new DeathTextTemplateAnalyzer();

        var result = analyzer.Analyze(320, 160, (x, y) =>
        {
            const double textScale = 0.60;
            var localX = (int)Math.Round((x - 124) / textScale);
            var localY = (int)Math.Round((y - 72) / textScale);
            var inText = IsSyntheticTextPixel(localX, localY);

            if (inText)
            {
                return new RgbPixel(150, 9, 18);
            }

            return new RgbPixel(24, 24, 24);
        }, [template], 0.8);

        Assert.True(result.IsMatch, $"score={result.Score:0.000}, scale={result.Scale:0.00}, method={result.Method}, details={result.Details}");
    }

    [Fact]
    public void IgnoresRedBackgroundWithoutTextShape()
    {
        var template = CreateTemplate();
        var analyzer = new DeathTextTemplateAnalyzer();

        var result = analyzer.Analyze(320, 160, (_, _) => new RgbPixel(165, 45, 25), [template], 0.75);

        Assert.False(result.IsMatch);
    }

    [Fact]
    public void IgnoresDarkNonTextShapeOnRedBackground()
    {
        var template = CreateTemplate();
        var analyzer = new DeathTextTemplateAnalyzer();

        var result = analyzer.Analyze(320, 160, (x, y) =>
        {
            var darkSmear = x is > 90 and < 230 && y is > 66 and < 92 && (x + y) % 5 != 0;
            return darkSmear ? new RgbPixel(30, 30, 30) : new RgbPixel(165, 45, 25);
        }, [template], 0.75);

        Assert.False(result.IsMatch, $"score={result.Score:0.000}, scale={result.Scale:0.00}, method={result.Method}");
    }

    [Fact]
    public void IgnoresRedBossHealthBarWithoutDeathTextShape()
    {
        var template = CreateTemplate();
        var analyzer = new DeathTextTemplateAnalyzer();

        var result = analyzer.Analyze(320, 160, (x, y) =>
        {
            var inBossBar = x is > 70 and < 250 && y is > 112 and < 118;
            var inBossBarFrame = x is > 68 and < 252 && y is > 109 and < 121;
            if (inBossBar)
            {
                return new RgbPixel(135, 15, 18);
            }

            return inBossBarFrame ? new RgbPixel(95, 84, 65) : new RgbPixel(32, 34, 36);
        }, [template], 0.75);

        Assert.False(result.IsMatch, $"score={result.Score:0.000}, scale={result.Scale:0.00}, method={result.Method}");
    }

    [Fact]
    public void IgnoresMatchingTextShapeOutsideDeathMessageRegion()
    {
        var template = CreateTemplate();
        var analyzer = new DeathTextTemplateAnalyzer();

        var result = analyzer.Analyze(320, 160, (x, y) =>
        {
            var inText = IsSyntheticTextPixel(x - 112, y - 18);
            return inText ? new RgbPixel(34, 34, 34) : new RgbPixel(165, 45, 25);
        }, [template], 0.75);

        Assert.False(result.IsMatch, $"score={result.Score:0.000}, scale={result.Scale:0.00}, method={result.Method}");
    }

    [Fact]
    public void IgnoresMatchingTextShapeBelowDeathMessageRegion()
    {
        var template = CreateTemplate();
        var analyzer = new DeathTextTemplateAnalyzer();

        var result = analyzer.Analyze(320, 160, (x, y) =>
        {
            var inText = IsSyntheticTextPixel(x - 112, y - 126);
            return inText ? new RgbPixel(130, 10, 22) : new RgbPixel(34, 34, 34);
        }, [template], 0.75);

        Assert.False(result.IsMatch, $"score={result.Score:0.000}, scale={result.Scale:0.00}, method={result.Method}, details={result.Details}");
    }

    private static DeathTextTemplate CreateTemplate()
    {
        return DeathTextTemplate.FromReference(
            "NIE ZYJESZ",
            96,
            32,
            (x, y) => IsSyntheticTextPixel(x, y)
                ? new RgbPixel(130, 10, 22)
                : new RgbPixel(90, 90, 90));
    }

    private static bool IsSyntheticTextPixel(int x, int y)
    {
        if (x < 0 || y < 0 || x >= 96 || y >= 32)
        {
            return false;
        }

        return InRect(x, y, 2, 6, 8, 20) ||
               InRect(x, y, 13, 6, 20, 20) ||
               InRect(x, y, 25, 6, 42, 12) ||
               InRect(x, y, 30, 13, 38, 20) ||
               InRect(x, y, 47, 6, 55, 20) ||
               InRect(x, y, 60, 6, 76, 12) ||
               InRect(x, y, 66, 13, 72, 20) ||
               InRect(x, y, 81, 6, 93, 20);
    }

    private static bool InRect(int x, int y, int left, int top, int right, int bottom)
    {
        return x >= left && x <= right && y >= top && y <= bottom;
    }
}
