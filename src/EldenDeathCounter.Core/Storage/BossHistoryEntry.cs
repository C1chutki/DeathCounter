namespace EldenDeathCounter.Core.Storage;

public sealed class BossHistoryEntry
{
    public string Name { get; set; } = string.Empty;

    public int DeathCount { get; set; }

    public DateTimeOffset StartedAt { get; set; }

    public DateTimeOffset DefeatedAt { get; set; }

    public TimeSpan KillDuration { get; set; }

    public string CompletedBy { get; set; } = string.Empty;
}
