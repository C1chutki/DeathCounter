using EldenDeathCounter.Core.Detection;

namespace EldenDeathCounter.Tests.Core;

public sealed class GameBossListFilesTests
{
    [Theory]
    [InlineData("DarkSouls1", "PL", "PL_DS1_BossList.txt")]
    [InlineData("DarkSouls2", "ENG", "ENG_DS2_BossList.txt")]
    [InlineData("DarkSouls3", "PL", "PL_DS3_BossList.txt")]
    [InlineData("DarkSouls3", "ENG", "ENG_DS3_BossList.txt")]
    public void ResolvesDarkSoulsListsFromAssetsRoot(string gameId, string language, string expected)
    {
        Assert.Equal(expected, GameBossListFiles.Resolve(gameId, language));
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
