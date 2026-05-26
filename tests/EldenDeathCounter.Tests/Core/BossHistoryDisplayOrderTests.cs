using EldenDeathCounter.Core.Storage;

namespace EldenDeathCounter.Tests.Core;

public sealed class BossHistoryDisplayOrderTests
{
    [Fact]
    public void CreatesNewestFirstDisplayWithOldestBossNumberedFirst()
    {
        var firstBoss = CreateBoss("Tree Sentinel", new DateTimeOffset(2026, 5, 24, 18, 0, 0, TimeSpan.FromHours(2)));
        var secondBoss = CreateBoss("Margit", new DateTimeOffset(2026, 5, 24, 19, 0, 0, TimeSpan.FromHours(2)));
        var thirdBoss = CreateBoss("Godrick", new DateTimeOffset(2026, 5, 24, 20, 0, 0, TimeSpan.FromHours(2)));

        var displayEntries = BossHistoryDisplayOrder.CreateNumberedEntries(
            [firstBoss, secondBoss, thirdBoss],
            searchText: string.Empty).ToList();

        Assert.Collection(
            displayEntries,
            entry =>
            {
                Assert.Same(thirdBoss, entry.Entry);
                Assert.Equal(3, entry.KillNumber);
            },
            entry =>
            {
                Assert.Same(secondBoss, entry.Entry);
                Assert.Equal(2, entry.KillNumber);
            },
            entry =>
            {
                Assert.Same(firstBoss, entry.Entry);
                Assert.Equal(1, entry.KillNumber);
            });
    }

    [Fact]
    public void PreservesOverallBossNumberWhenSearchFiltersHistory()
    {
        var firstBoss = CreateBoss("Tree Sentinel", new DateTimeOffset(2026, 5, 24, 18, 0, 0, TimeSpan.FromHours(2)));
        var secondBoss = CreateBoss("Margit", new DateTimeOffset(2026, 5, 24, 19, 0, 0, TimeSpan.FromHours(2)));
        var thirdBoss = CreateBoss("Godrick", new DateTimeOffset(2026, 5, 24, 20, 0, 0, TimeSpan.FromHours(2)));

        var displayEntry = Assert.Single(BossHistoryDisplayOrder.CreateNumberedEntries(
            [firstBoss, secondBoss, thirdBoss],
            searchText: "Tree"));

        Assert.Same(firstBoss, displayEntry.Entry);
        Assert.Equal(1, displayEntry.KillNumber);
    }

    private static BossHistoryEntry CreateBoss(string name, DateTimeOffset defeatedAt)
    {
        return new BossHistoryEntry
        {
            Name = name,
            DeathCount = 1,
            StartedAt = defeatedAt.AddMinutes(-5),
            DefeatedAt = defeatedAt,
            KillDuration = TimeSpan.FromMinutes(5),
            CompletedBy = "manual-hotkey"
        };
    }
}
