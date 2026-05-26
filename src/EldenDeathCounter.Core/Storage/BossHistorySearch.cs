namespace EldenDeathCounter.Core.Storage;

public static class BossHistorySearch
{
    public static bool Matches(BossHistoryEntry boss, string? searchText)
    {
        if (string.IsNullOrWhiteSpace(searchText))
        {
            return true;
        }

        return boss.Name.Contains(searchText.Trim(), StringComparison.OrdinalIgnoreCase);
    }
}
