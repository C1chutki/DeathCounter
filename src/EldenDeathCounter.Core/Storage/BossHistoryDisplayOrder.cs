namespace EldenDeathCounter.Core.Storage;

public static class BossHistoryDisplayOrder
{
    public static IEnumerable<NumberedBossHistoryEntry> CreateNumberedEntries(
        IReadOnlyList<BossHistoryEntry> bossHistory,
        string? searchText)
    {
        return bossHistory
            .Select((entry, index) => new
            {
                Entry = entry,
                OriginalIndex = index
            })
            .OrderBy(item => item.Entry.DefeatedAt)
            .ThenBy(item => item.OriginalIndex)
            .Select((item, index) => new
            {
                item.Entry,
                KillNumber = index + 1
            })
            .Where(item => BossHistorySearch.Matches(item.Entry, searchText))
            .OrderByDescending(item => item.Entry.DefeatedAt)
            .Select(item => new NumberedBossHistoryEntry(item.Entry, item.KillNumber));
    }
}

public sealed record NumberedBossHistoryEntry(BossHistoryEntry Entry, int KillNumber);
