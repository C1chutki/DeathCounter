using EldenDeathCounter.Core.Storage;

namespace EldenDeathCounter.Tests.Core;

public sealed class BossHistorySearchTests
{
    [Theory]
    [InlineData("", true)]
    [InlineData("   ", true)]
    [InlineData("marg", true)]
    [InlineData("MARGIT", true)]
    [InlineData("godrick", false)]
    public void MatchesBossNamesByCaseInsensitivePartialText(string searchText, bool expected)
    {
        var boss = new BossHistoryEntry
        {
            Name = "Margit, The Fell Omen"
        };

        Assert.Equal(expected, BossHistorySearch.Matches(boss, searchText));
    }
}
