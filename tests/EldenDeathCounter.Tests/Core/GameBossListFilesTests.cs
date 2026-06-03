using EldenDeathCounter.Core.Detection;

namespace EldenDeathCounter.Tests.Core;

public sealed class GameBossListFilesTests
{
    [Theory]
    [InlineData("EldenRing", "PL", "PL_BossList.txt")]
    [InlineData("EldenRing", "ENG", "ENG_BossList.txt")]
    [InlineData("DarkSouls1", "PL", "PL_DS1_BossList.txt")]
    [InlineData("DarkSouls2", "ENG", "ENG_DS2_BossList.txt")]
    [InlineData("DarkSouls3", "PL", "PL_DS3_BossList.txt")]
    [InlineData("DarkSouls3", "ENG", "ENG_DS3_BossList.txt")]
    public void ResolvesPerGameAndLanguage(string gameId, string language, string expected)
    {
        Assert.Equal(expected, GameBossListFiles.Resolve(gameId, language));
    }

    [Fact]
    public void UnknownOrEmptyGameFallsBackToEldenRingList()
    {
        Assert.Equal("ENG_BossList.txt", GameBossListFiles.Resolve(null, "ENG"));
        Assert.Equal("PL_BossList.txt", GameBossListFiles.Resolve("", "PL"));
    }

    [Fact]
    public void GameIdIsCaseInsensitive()
    {
        Assert.Equal("PL_DS3_BossList.txt", GameBossListFiles.Resolve("darksouls3", "PL"));
    }
}
