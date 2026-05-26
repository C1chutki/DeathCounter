using EldenDeathCounter.Core.Detection;

namespace EldenDeathCounter.Tests.Core;

public sealed class DeathPhraseMatcherTests
{
    [Fact]
    public void FindsEnglishDeathPhraseInOcrText()
    {
        var matcher = new DeathPhraseMatcher();

        var result = matcher.Match("ashes gather\nYOU DIED\nreturning to last site of grace", ["YOU DIED", "NIE ŻYJESZ"], 0.8);

        Assert.True(result.IsMatch);
        Assert.Equal("YOU DIED", result.MatchedPhrase);
    }

    [Fact]
    public void FindsPolishDeathPhraseWithoutDependingOnDiacritics()
    {
        var matcher = new DeathPhraseMatcher();

        var result = matcher.Match("NIE ZYJESZ", ["YOU DIED", "NIE ŻYJESZ"], 0.8);

        Assert.True(result.IsMatch);
        Assert.Equal("NIE ŻYJESZ", result.MatchedPhrase);
    }

    [Fact]
    public void DoesNotMatchUnrelatedScreenText()
    {
        var matcher = new DeathPhraseMatcher();

        var result = matcher.Match("Inventory Equipment Map Status", ["YOU DIED", "NIE ŻYJESZ"], 0.85);

        Assert.False(result.IsMatch);
    }

    [Fact]
    public void DoesNotMatchOwnSettingsPhraseList()
    {
        var matcher = new DeathPhraseMatcher();

        var result = matcher.Match(
            "Detection phrases\r\nYOU DIED\r\nNIE ZYJESZ\r\nStart detection",
            ["YOU DIED", "NIE ŻYJESZ"],
            0.8);

        Assert.False(result.IsMatch);
    }

    [Fact]
    public void CanMatchBossVictoryPhraseWhenOwnWindowTextIsAlsoVisible()
    {
        var matcher = new DeathPhraseMatcher();

        var result = matcher.Match(
            "Detection sensitivity\r\nGREATENEMYFELLED\r\nRadagon Icon",
            ["ENEMY FELLED", "GREAT ENEMY FELLED", "GOD SLAIN"],
            0.8,
            suppressOwnSettingsTextGuard: true);

        Assert.True(result.IsMatch);
        Assert.Equal("GREAT ENEMY FELLED", result.MatchedPhrase);
    }
}
