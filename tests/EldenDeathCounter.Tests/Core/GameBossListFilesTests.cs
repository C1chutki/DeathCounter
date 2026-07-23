using EldenDeathCounter.Core.Detection;

namespace EldenDeathCounter.Tests.Core;

public sealed class GameBossListFilesTests
{
    [Theory]
    [InlineData("DarkSouls1", "PL", "PL_DS1_BossList.txt")]
    [InlineData("DarkSouls3", "PL", "PL_DS3_BossList.txt")]
    [InlineData("DarkSouls3", "ENG", "ENG_DS3_BossList.txt")]
    public void ResolvesDarkSoulsListsFromAssetsRoot(string gameId, string language, string expected)
    {
        Assert.Equal(expected, GameBossListFiles.Resolve(gameId, language));
    }

    [Theory]
    [InlineData("ENG", "ENG_DS2_BossList.txt")]
    [InlineData("PL", "PL_DS2_BossList.txt")]
    public void ResolvesDarkSouls2ListsFromDarkSouls2Subfolder(string language, string fileName)
    {
        // The DS2 lists were relocated to the "Dark souls 2" subfolder (no duplicate at Assets root).
        Assert.Equal(Path.Combine("Dark souls 2", fileName), GameBossListFiles.Resolve("DarkSouls2", language));
    }

    [Fact]
    public void DarkSouls2MatcherLoadsSelectedLanguageAndEnglishBossListsWhenPolish()
    {
        // DS2 shows English boss names in-game even when the app is Polish, so the PL matcher also
        // includes the English list (mirroring the Elden Ring multi-list mechanism).
        var pl = GameBossListFiles.ResolveForMatcher("DarkSouls2", "PL", BossHealthBarStyles.Vanilla);
        var eng = GameBossListFiles.ResolveForMatcher("DarkSouls2", "ENG", BossHealthBarStyles.Vanilla);

        Assert.Equal(
            [
                Path.Combine("Dark souls 2", "PL_DS2_BossList.txt"),
                Path.Combine("Dark souls 2", "ENG_DS2_BossList.txt")
            ],
            pl);
        Assert.Equal([Path.Combine("Dark souls 2", "ENG_DS2_BossList.txt")], eng);
    }

    [Theory]
    [InlineData("EldenRing", "PL", "PL_ER_BossList.txt")]
    [InlineData("EldenRing", "ENG", "ENG_ER_BossList.txt")]
    public void ResolvesEldenRingListsFromEldenRingSubfolder(string gameId, string language, string fileName)
    {
        Assert.Equal(Path.Combine("Elden Ring", fileName), GameBossListFiles.Resolve(gameId, language));
    }

    [Fact]
    public void UnknownOrEmptyGameFallsBackToEldenRingList()
    {
        Assert.Equal(Path.Combine("Elden Ring", "ENG_ER_BossList.txt"), GameBossListFiles.Resolve(null, "ENG"));
        Assert.Equal(Path.Combine("Elden Ring", "PL_ER_BossList.txt"), GameBossListFiles.Resolve("", "PL"));
    }

    [Fact]
    public void GameIdIsCaseInsensitive()
    {
        Assert.Equal("PL_DS3_BossList.txt", GameBossListFiles.Resolve("darksouls3", "PL"));
    }

    [Fact]
    public void ReforgedEldenRingMatcherLoadsSelectedLanguageAndEnglishBossLists()
    {
        var files = GameBossListFiles.ResolveForMatcher("EldenRing", "PL", BossHealthBarStyles.Reforged);

        Assert.Equal(
            [
                Path.Combine("Elden Ring", "PL_ER_BossList.txt"),
                Path.Combine("Elden Ring", "ENG_ER_BossList.txt")
            ],
            files);
    }

    [Fact]
    public void ConvergenceEldenRingMatcherLoadsSelectedLanguageAndConvergenceBossLists()
    {
        var files = GameBossListFiles.ResolveForMatcher("EldenRing", "PL", BossHealthBarStyles.Convergence);

        Assert.Equal(
            [
                Path.Combine("Elden Ring", "PL_ER_BossList.txt"),
                Path.Combine("Elden Ring", "ENG_ER_BossList.txt"),
                Path.Combine("Elden Ring", "Convergence", "ENG_ER_Convergence_BossList.txt")
            ],
            files);
    }

    [Fact]
    public void VanillaMatcherLoadsOnlySelectedLanguageBossList()
    {
        var files = GameBossListFiles.ResolveForMatcher("EldenRing", "PL", BossHealthBarStyles.Vanilla);

        Assert.Equal([Path.Combine("Elden Ring", "PL_ER_BossList.txt")], files);
    }
}
