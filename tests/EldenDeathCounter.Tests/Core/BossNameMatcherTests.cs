using EldenDeathCounter.Core.Detection;

namespace EldenDeathCounter.Tests.Core;

public sealed class BossNameMatcherTests
{
    private static BossNameMatcher CreateEnglishMatcher()
    {
        var lines = File.ReadAllLines(GetAssetPath("ENG_BossList.txt"));
        return new BossNameMatcher(BossNameMatcher.ParseList(lines));
    }

    [Fact]
    public void ParseListIgnoresBlankAndCommentLinesAndTrims()
    {
        var names = BossNameMatcher.ParseList(new[] { "  Margit, the Fell Omen ", "", "   ", "# comment", "Godrick the Grafted" });

        Assert.Equal(new[] { "Margit, the Fell Omen", "Godrick the Grafted" }, names);
    }

    [Fact]
    public void EnglishBossListLoadsManyNames()
    {
        var matcher = CreateEnglishMatcher();

        Assert.True(matcher.HasEntries);
        Assert.True(matcher.Count > 100);
    }

    [Fact]
    public void MatchesExactBossName()
    {
        var matcher = CreateEnglishMatcher();

        var match = matcher.Match("Margit, the Fell Omen");

        Assert.True(match.IsMatch);
        Assert.Equal("Margit, the Fell Omen", match.CanonicalName);
        Assert.True(match.Confidence >= 0.99);
    }

    [Fact]
    public void MatchesCaseInsensitively()
    {
        var matcher = CreateEnglishMatcher();

        var match = matcher.Match("godrick the grafted");

        Assert.True(match.IsMatch);
        Assert.Equal("Godrick the Grafted", match.CanonicalName);
    }

    [Fact]
    public void MatchesAfterWhitespaceNormalization()
    {
        var matcher = CreateEnglishMatcher();

        var match = matcher.Match("   Godrick    the   Grafted  ");

        Assert.True(match.IsMatch);
        Assert.Equal("Godrick the Grafted", match.CanonicalName);
    }

    [Fact]
    public void MatchesBaseNameWhenOcrIncludesParentheticalDisambiguator()
    {
        var matcher = CreateEnglishMatcher();

        var match = matcher.Match("Crystalian (Spear)");

        Assert.True(match.IsMatch);
        Assert.Equal("Crystalian", match.CanonicalName);
        // The disambiguator is preserved for display.
        Assert.Equal("Crystalian (Spear)", match.DisplayName);
    }

    [Fact]
    public void MatchesMildlyCorruptedOcrWhenConfidenceIsHighEnough()
    {
        var matcher = CreateEnglishMatcher();

        // "Fell" misread as "Fel", missing comma.
        var match = matcher.Match("Margit the Fel Omen");

        Assert.True(match.IsMatch);
        Assert.Equal("Margit, the Fell Omen", match.CanonicalName);
    }

    [Theory]
    [InlineData("rd Âv")]
    [InlineData("Rotihdtåblékmgh V")]
    [InlineData("Cruäßle")]
    [InlineData("asdfghjkl")]
    [InlineData("xqz")]
    public void RejectsGarbageOcr(string garbage)
    {
        var matcher = CreateEnglishMatcher();

        var match = matcher.Match(garbage);

        Assert.False(match.IsMatch);
    }

    [Theory]
    [InlineData("Talk")]
    [InlineData("Sit")]
    [InlineData("Rest")]
    [InlineData("Read message")]
    [InlineData("Touch bloodstain")]
    [InlineData("Switch action")]
    [InlineData("Examine")]
    [InlineData("Open")]
    [InlineData("Pillage corpse")]
    [InlineData("Retrieve lost runes")]
    [InlineData("Bloody Slash")]
    public void RejectsInteractionPromptsAndHudText(string prompt)
    {
        var matcher = CreateEnglishMatcher();

        var match = matcher.Match(prompt);

        Assert.False(match.IsMatch);
    }

    [Fact]
    public void RejectsEmptyOrWhitespace()
    {
        var matcher = CreateEnglishMatcher();

        Assert.False(matcher.Match("").IsMatch);
        Assert.False(matcher.Match("   ").IsMatch);
    }

    [Fact]
    public void EmptyMatcherNeverMatches()
    {
        var matcher = new BossNameMatcher(BossNameMatcher.ParseList(Array.Empty<string>()));

        Assert.False(matcher.HasEntries);
        Assert.False(matcher.Match("Margit, the Fell Omen").IsMatch);
    }

    private static string GetAssetPath(string assetName)
    {
        return Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory, "..", "..", "..", "..", "..", "Assets", assetName));
    }
}
