using EldenDeathCounter.Core.Configuration;

namespace EldenDeathCounter.Tests.Core;

public sealed class AppGameThemeTests
{
    [Fact]
    public void DarkSouls3ThemeUsesRequestedTitleAndPrimaryColor()
    {
        var theme = AppGameTheme.DarkSouls3;

        Assert.Equal("Dark Souls 3 Death Counter", theme.Title);
        Assert.Equal("#E65100", theme.Primary);
        Assert.Equal("#424242", theme.Secondary);
        Assert.Equal("#CFD8DC", theme.Tertiary);
        Assert.Equal("#0A0A0A", theme.Neutral);
    }

    [Fact]
    public void DarkSouls1ThemeUsesRequestedTitleAndBluePalette()
    {
        var theme = AppGameTheme.DarkSouls1;

        Assert.Equal("Dark Souls Death Counter", theme.Title);
        Assert.Equal("#4A90E2", theme.Primary);
        Assert.Equal("#1A1A1A", theme.Tertiary);
        Assert.Equal("#0A0A0A", theme.Neutral);
        Assert.Equal("#C2C2C2", theme.MutedInk);
        Assert.Equal(theme.Primary, theme.OverlayBorder);
    }

    [Fact]
    public void DarkSouls2ThemeUsesRequestedTitleAndIndigoPalette()
    {
        var theme = AppGameTheme.DarkSouls2;

        Assert.Equal("Dark Souls 2 Death Counter", theme.Title);
        Assert.Equal("#9FA8DA", theme.Primary);
        Assert.Equal("#1A237E", theme.Secondary);
        Assert.Equal("#B2DFDB", theme.Tertiary);
        Assert.Equal("#121212", theme.Neutral);
        Assert.Equal(theme.Primary, theme.OverlayBorder);
        Assert.Equal("#CC12152E", theme.OverlayBackground);
    }

    [Fact]
    public void EldenRingThemeUsesGoldBorderAndTransparentBackground()
    {
        var theme = AppGameTheme.EldenRing;

        Assert.Equal("#D9B45A", theme.OverlayBorder);
        Assert.Equal(theme.Primary, theme.OverlayBorder);
        Assert.Equal("#7F000000", theme.OverlayBackground);
    }
}
