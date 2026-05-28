namespace EldenDeathCounter.Core.Storage;

public enum BossHistorySortMode
{
    Default,
    Time,
    Deaths
}

public enum BossHistorySortDirection
{
    Ascending,
    Descending
}

public static class BossHistoryDisplayOrder
{
    public static IEnumerable<NumberedBossHistoryEntry> CreateNumberedEntries(
        IReadOnlyList<BossHistoryEntry> bossHistory,
        string? searchText,
        BossHistorySortMode sortMode = BossHistorySortMode.Default,
        BossHistorySortDirection sortDirection = BossHistorySortDirection.Descending)
    {
        var numbered = bossHistory
            .Select((entry, index) => new
            {
                Entry = entry,
                OriginalIndex = index
            })
            .OrderBy(item => item.Entry.DefeatedAt)
            .ThenBy(item => item.OriginalIndex)
            .Select((item, index) => new NumberedBossHistoryEntry(item.Entry, index + 1))
            .Where(item => BossHistorySearch.Matches(item.Entry, searchText))
            .ToList();

        return sortMode switch
        {
            BossHistorySortMode.Time => SortByDuration(numbered, sortDirection),
            BossHistorySortMode.Deaths => SortByDeaths(numbered, sortDirection),
            _ => SortByDefault(numbered, sortDirection)
        };
    }

    public static TimeSpan? GetSortableKillDuration(BossHistoryEntry entry)
    {
        if (entry.KillDuration > TimeSpan.Zero)
        {
            return entry.KillDuration;
        }

        var elapsed = entry.DefeatedAt - entry.StartedAt;
        return elapsed > TimeSpan.Zero ? elapsed : null;
    }

    private static IEnumerable<NumberedBossHistoryEntry> SortByDefault(
        IReadOnlyList<NumberedBossHistoryEntry> entries,
        BossHistorySortDirection direction)
    {
        return direction == BossHistorySortDirection.Ascending
            ? entries.OrderBy(item => item.Entry.DefeatedAt)
            : entries.OrderByDescending(item => item.Entry.DefeatedAt);
    }

    private static IEnumerable<NumberedBossHistoryEntry> SortByDuration(
        IReadOnlyList<NumberedBossHistoryEntry> entries,
        BossHistorySortDirection direction)
    {
        var withDuration = entries.Select(item => new
        {
            Item = item,
            Duration = GetSortableKillDuration(item.Entry)
        });

        var ordered = withDuration.OrderBy(item => item.Duration.HasValue ? 0 : 1);
        ordered = direction == BossHistorySortDirection.Ascending
            ? ordered.ThenBy(item => item.Duration ?? TimeSpan.Zero)
            : ordered.ThenByDescending(item => item.Duration ?? TimeSpan.Zero);

        return ordered
            .ThenByDescending(item => item.Item.Entry.DefeatedAt)
            .Select(item => item.Item);
    }

    private static IEnumerable<NumberedBossHistoryEntry> SortByDeaths(
        IReadOnlyList<NumberedBossHistoryEntry> entries,
        BossHistorySortDirection direction)
    {
        var ordered = direction == BossHistorySortDirection.Ascending
            ? entries.OrderBy(item => item.Entry.DeathCount)
            : entries.OrderByDescending(item => item.Entry.DeathCount);

        return ordered.ThenByDescending(item => item.Entry.DefeatedAt);
    }
}

public sealed record NumberedBossHistoryEntry(BossHistoryEntry Entry, int KillNumber);
