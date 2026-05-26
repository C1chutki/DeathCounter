namespace EldenDeathCounter.Core.Storage;

public sealed class DeathEvent
{
    public DateTimeOffset Timestamp { get; set; }

    public string DetectionMethod { get; set; } = string.Empty;

    public string Note { get; set; } = string.Empty;

    public int CountAfter { get; set; }

    public string? BossName { get; set; }

    public int? BossDeathCountAfter { get; set; }
}
