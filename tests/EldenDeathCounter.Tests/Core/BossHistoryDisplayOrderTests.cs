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

    [Fact]
    public void DefaultSortMatchesNewestFirstWithDescendingDirection()
    {
        var firstBoss = CreateBoss("Tree Sentinel", new DateTimeOffset(2026, 5, 24, 18, 0, 0, TimeSpan.FromHours(2)));
        var secondBoss = CreateBoss("Margit", new DateTimeOffset(2026, 5, 24, 19, 0, 0, TimeSpan.FromHours(2)));
        var thirdBoss = CreateBoss("Godrick", new DateTimeOffset(2026, 5, 24, 20, 0, 0, TimeSpan.FromHours(2)));

        var displayEntries = BossHistoryDisplayOrder.CreateNumberedEntries(
            [firstBoss, secondBoss, thirdBoss],
            searchText: string.Empty,
            BossHistorySortMode.Default,
            BossHistorySortDirection.Descending).ToList();

        Assert.Equal([thirdBoss, secondBoss, firstBoss], displayEntries.Select(e => e.Entry));
    }

    [Fact]
    public void DefaultSortAscendingShowsOldestFirst()
    {
        var firstBoss = CreateBoss("Tree Sentinel", new DateTimeOffset(2026, 5, 24, 18, 0, 0, TimeSpan.FromHours(2)));
        var secondBoss = CreateBoss("Margit", new DateTimeOffset(2026, 5, 24, 19, 0, 0, TimeSpan.FromHours(2)));
        var thirdBoss = CreateBoss("Godrick", new DateTimeOffset(2026, 5, 24, 20, 0, 0, TimeSpan.FromHours(2)));

        var displayEntries = BossHistoryDisplayOrder.CreateNumberedEntries(
            [firstBoss, secondBoss, thirdBoss],
            searchText: string.Empty,
            BossHistorySortMode.Default,
            BossHistorySortDirection.Ascending).ToList();

        Assert.Equal([firstBoss, secondBoss, thirdBoss], displayEntries.Select(e => e.Entry));
    }

    [Fact]
    public void SortByTimeAscendingPutsShortestFightFirst()
    {
        var shortFight = CreateBossWithDuration("Margit", TimeSpan.FromMinutes(2));
        var longFight = CreateBossWithDuration("Godrick", TimeSpan.FromMinutes(20));
        var mediumFight = CreateBossWithDuration("Radahn", TimeSpan.FromMinutes(8));

        var displayEntries = BossHistoryDisplayOrder.CreateNumberedEntries(
            [longFight, shortFight, mediumFight],
            searchText: string.Empty,
            BossHistorySortMode.Time,
            BossHistorySortDirection.Ascending).ToList();

        Assert.Equal([shortFight, mediumFight, longFight], displayEntries.Select(e => e.Entry));
    }

    [Fact]
    public void SortByTimeDescendingPutsLongestFightFirst()
    {
        var shortFight = CreateBossWithDuration("Margit", TimeSpan.FromMinutes(2));
        var longFight = CreateBossWithDuration("Godrick", TimeSpan.FromMinutes(20));
        var mediumFight = CreateBossWithDuration("Radahn", TimeSpan.FromMinutes(8));

        var displayEntries = BossHistoryDisplayOrder.CreateNumberedEntries(
            [longFight, shortFight, mediumFight],
            searchText: string.Empty,
            BossHistorySortMode.Time,
            BossHistorySortDirection.Descending).ToList();

        Assert.Equal([longFight, mediumFight, shortFight], displayEntries.Select(e => e.Entry));
    }

    [Fact]
    public void SortByDeathsAscendingPutsLowestCountFirst()
    {
        var fewDeaths = CreateBossWithDeaths("Margit", 3);
        var manyDeaths = CreateBossWithDeaths("Malenia", 99);
        var someDeaths = CreateBossWithDeaths("Radahn", 17);

        var displayEntries = BossHistoryDisplayOrder.CreateNumberedEntries(
            [manyDeaths, fewDeaths, someDeaths],
            searchText: string.Empty,
            BossHistorySortMode.Deaths,
            BossHistorySortDirection.Ascending).ToList();

        Assert.Equal([fewDeaths, someDeaths, manyDeaths], displayEntries.Select(e => e.Entry));
    }

    [Fact]
    public void SortByDeathsDescendingPutsHighestCountFirst()
    {
        var fewDeaths = CreateBossWithDeaths("Margit", 3);
        var manyDeaths = CreateBossWithDeaths("Malenia", 99);
        var someDeaths = CreateBossWithDeaths("Radahn", 17);

        var displayEntries = BossHistoryDisplayOrder.CreateNumberedEntries(
            [manyDeaths, fewDeaths, someDeaths],
            searchText: string.Empty,
            BossHistorySortMode.Deaths,
            BossHistorySortDirection.Descending).ToList();

        Assert.Equal([manyDeaths, someDeaths, fewDeaths], displayEntries.Select(e => e.Entry));
    }

    [Fact]
    public void SortByTimePlacesMissingDurationLastInBothDirections()
    {
        var withDuration = CreateBossWithDuration("Margit", TimeSpan.FromMinutes(5));
        var missingDuration = CreateBossWithDuration("Mystery", TimeSpan.Zero);
        missingDuration.StartedAt = missingDuration.DefeatedAt;

        var ascending = BossHistoryDisplayOrder.CreateNumberedEntries(
            [missingDuration, withDuration],
            searchText: string.Empty,
            BossHistorySortMode.Time,
            BossHistorySortDirection.Ascending).ToList();
        Assert.Equal([withDuration, missingDuration], ascending.Select(e => e.Entry));

        var descending = BossHistoryDisplayOrder.CreateNumberedEntries(
            [missingDuration, withDuration],
            searchText: string.Empty,
            BossHistorySortMode.Time,
            BossHistorySortDirection.Descending).ToList();
        Assert.Equal([withDuration, missingDuration], descending.Select(e => e.Entry));
    }

    [Fact]
    public void SortByDeathsTreatsZeroCountAsLowest()
    {
        var zeroDeaths = CreateBossWithDeaths("Soldier", 0);
        var someDeaths = CreateBossWithDeaths("Radahn", 12);

        var ascending = BossHistoryDisplayOrder.CreateNumberedEntries(
            [someDeaths, zeroDeaths],
            searchText: string.Empty,
            BossHistorySortMode.Deaths,
            BossHistorySortDirection.Ascending).ToList();

        Assert.Equal([zeroDeaths, someDeaths], ascending.Select(e => e.Entry));
    }

    [Fact]
    public void SortAppliesTogetherWithSearchFilter()
    {
        var margitShort = CreateBossWithDuration("Margit, The Fell Omen", TimeSpan.FromMinutes(2));
        var margitLong = CreateBossWithDuration("Margit Clone", TimeSpan.FromMinutes(15));
        var godrick = CreateBossWithDuration("Godrick", TimeSpan.FromMinutes(8));

        var displayEntries = BossHistoryDisplayOrder.CreateNumberedEntries(
            [margitLong, godrick, margitShort],
            searchText: "Margit",
            BossHistorySortMode.Time,
            BossHistorySortDirection.Ascending).ToList();

        Assert.Equal([margitShort, margitLong], displayEntries.Select(e => e.Entry));
    }

    [Fact]
    public void ChangingSortModeDoesNotMutateUnderlyingHistoryOrder()
    {
        var firstBoss = CreateBossWithDeaths("Margit", 3);
        var secondBoss = CreateBossWithDeaths("Malenia", 99);
        var thirdBoss = CreateBossWithDeaths("Radahn", 17);
        var history = new List<BossHistoryEntry> { firstBoss, secondBoss, thirdBoss };

        _ = BossHistoryDisplayOrder.CreateNumberedEntries(
            history,
            searchText: string.Empty,
            BossHistorySortMode.Deaths,
            BossHistorySortDirection.Descending).ToList();

        Assert.Equal([firstBoss, secondBoss, thirdBoss], history);
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

    private static BossHistoryEntry CreateBossWithDuration(string name, TimeSpan duration)
    {
        var defeatedAt = new DateTimeOffset(2026, 5, 24, 20, 0, 0, TimeSpan.FromHours(2));
        return new BossHistoryEntry
        {
            Name = name,
            DeathCount = 1,
            StartedAt = defeatedAt - duration,
            DefeatedAt = defeatedAt,
            KillDuration = duration,
            CompletedBy = "manual-hotkey"
        };
    }

    private static BossHistoryEntry CreateBossWithDeaths(string name, int deathCount)
    {
        var defeatedAt = new DateTimeOffset(2026, 5, 24, 20, 0, 0, TimeSpan.FromHours(2));
        return new BossHistoryEntry
        {
            Name = name,
            DeathCount = deathCount,
            StartedAt = defeatedAt.AddMinutes(-5),
            DefeatedAt = defeatedAt,
            KillDuration = TimeSpan.FromMinutes(5),
            CompletedBy = "manual-hotkey"
        };
    }
}
