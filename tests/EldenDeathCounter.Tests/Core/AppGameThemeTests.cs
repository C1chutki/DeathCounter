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
}
