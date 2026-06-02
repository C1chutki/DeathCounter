using EldenDeathCounter.Core.Configuration;

namespace EldenDeathCounter.Tests.Core;

public sealed class AppGameThemeTests
{
    [Fact]
    public void DarkSoulsThemesMatchEldenRingPaletteExceptTitleAndPrimaryAccent()
    {
        AssertThemeMatchesEldenRingPalette(AppGameTheme.DarkSouls1, "Dark Souls Death Counter", "#4A90E2");
        AssertThemeMatchesEldenRingPalette(AppGameTheme.DarkSouls2, "Dark Souls 2 Death Counter", "#9FA8DA");
        AssertThemeMatchesEldenRingPalette(AppGameTheme.DarkSouls3, "Dark Souls 3 Death Counter", "#E65100");
    }

    [Fact]
    public void EldenRingThemeUsesGoldBorderAndTransparentBackground()
    {
        var theme = AppGameTheme.EldenRing;

        Assert.Equal("#D9B45A", theme.OverlayBorder);
        Assert.Equal(theme.Primary, theme.OverlayBorder);
        Assert.Equal("#7F000000", theme.OverlayBackground);
    }

    private static void AssertThemeMatchesEldenRingPalette(AppGameTheme theme, string title, string primary)
    {
        var eldenRing = AppGameTheme.EldenRing;

        Assert.Equal(title, theme.Title);
        Assert.Equal(primary, theme.Primary);
        Assert.Equal(eldenRing.Secondary, theme.Secondary);
        Assert.Equal(eldenRing.Tertiary, theme.Tertiary);
        Assert.Equal(eldenRing.Neutral, theme.Neutral);
        Assert.Equal(eldenRing.Ink, theme.Ink);
        Assert.Equal(eldenRing.MutedInk, theme.MutedInk);
        Assert.Equal(eldenRing.Panel, theme.Panel);
        Assert.Equal(eldenRing.PanelAlt, theme.PanelAlt);
        Assert.Equal(eldenRing.Border, theme.Border);
        Assert.Equal(eldenRing.OverlayBackground, theme.OverlayBackground);
        Assert.Equal(theme.Primary, theme.OverlayBorder);
        Assert.Equal(eldenRing.OverlayText, theme.OverlayText);
    }
}
