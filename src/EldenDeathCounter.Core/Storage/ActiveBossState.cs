namespace EldenDeathCounter.Core.Storage;

public sealed class ActiveBossState
{
    public string Name { get; set; } = string.Empty;

    public int DeathCount { get; set; }

    public DateTimeOffset StartedAt { get; set; } = DateTimeOffset.Now;

    public TimeSpan ElapsedBeforeCurrentRun { get; set; }

    public DateTimeOffset? TimerStartedAt { get; set; }

    public bool IsTimerRunning { get; set; }

    public TimeSpan GetElapsedDuration(DateTimeOffset now)
    {
        var duration = ElapsedBeforeCurrentRun;
        if (IsTimerRunning && TimerStartedAt is not null)
        {
            duration += now - TimerStartedAt.Value;
        }

        return duration < TimeSpan.Zero ? TimeSpan.Zero : duration;
    }
}
