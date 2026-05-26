using EldenDeathCounter.Core.Configuration;

namespace EldenDeathCounter.Tests.Core;

public sealed class DeathCounterTextTests
{
    [Theory]
    [InlineData("PL", 7, "\u015Amierci: 7")]
    [InlineData("ENG", 7, "Deaths: 7")]
    [InlineData("en", 7, "Deaths: 7")]
    [InlineData("", 7, "\u015Amierci: 7")]
    public void FormatsGlobalDeathCounterForGameLanguage(string language, int count, string expected)
    {
        Assert.Equal(expected, DeathCounterText.FormatGlobalCount(count, language));
    }

    [Theory]
    [InlineData("PL", "\u015Amierci")]
    [InlineData("ENG", "Deaths")]
    [InlineData("en", "Deaths")]
    [InlineData("", "\u015Amierci")]
    public void FormatsDeathLabelForGameLanguage(string language, string expected)
    {
        Assert.Equal(expected, DeathCounterText.FormatDeathLabel(language));
    }

    [Fact]
    public void FormatsTwoBossNamesOnSeparateLines()
    {
        Assert.Equal(
            $"Godskin Apostle{Environment.NewLine}+{Environment.NewLine}Godskin Noble",
            DeathCounterText.FormatBossOverlayName("Godskin Apostle + Godskin Noble"));
    }
}
