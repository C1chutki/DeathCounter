namespace EldenDeathCounter.Core.Storage;

public static class CounterStatsService
{
    public static CounterStatsSummary CreateSummary(
        DeathCounterState state,
        DateTimeOffset sessionStartedAt,
        DateTimeOffset now)
    {
        var positiveEvents = GetPositiveDeathEvents(state.DeathEvents);
        var deathsToday = positiveEvents.Count(item => item.Timestamp.Date == now.Date);
        var sessionDeaths = positiveEvents.Count(item => item.Timestamp >= sessionStartedAt && item.Timestamp <= now);
        var sessionHours = Math.Max(1.0 / 60.0, (now - sessionStartedAt).TotalHours);
        var best = state.BossHistory
            .OrderBy(item => item.DeathCount)
            .ThenBy(GetBossKillDuration)
            .ThenBy(item => item.Name, StringComparer.CurrentCultureIgnoreCase)
            .FirstOrDefault();
        var hardest = state.BossHistory
            .OrderByDescending(item => item.DeathCount)
            .ThenBy(item => item.Name, StringComparer.CurrentCultureIgnoreCase)
            .FirstOrDefault();
        var longest = state.BossHistory
            .OrderByDescending(GetBossKillDuration)
            .ThenByDescending(item => item.DeathCount)
            .ThenBy(item => item.Name, StringComparer.CurrentCultureIgnoreCase)
            .FirstOrDefault();

        return new CounterStatsSummary(
            state.CurrentDeathCount,
            deathsToday,
            sessionDeaths,
            sessionDeaths / sessionHours,
            state.ActiveBoss?.Name ?? string.Empty,
            state.ActiveBoss?.DeathCount ?? 0,
            best?.Name ?? string.Empty,
            best?.DeathCount ?? 0,
            best is null ? TimeSpan.Zero : GetBossKillDuration(best),
            hardest?.Name ?? string.Empty,
            hardest?.DeathCount ?? 0,
            hardest is null ? TimeSpan.Zero : GetBossKillDuration(hardest),
            longest?.Name ?? string.Empty,
            longest?.DeathCount ?? 0,
            longest is null ? TimeSpan.Zero : GetBossKillDuration(longest),
            state.DeathEvents
                .OrderByDescending(item => item.Timestamp)
                .Take(3)
                .ToList());
    }

    private static IReadOnlyList<DeathEvent> GetPositiveDeathEvents(IReadOnlyList<DeathEvent> events)
    {
        var positive = new List<DeathEvent>();
        var previousCount = 0;
        foreach (var current in events.OrderBy(item => item.Timestamp))
        {
            if (current.CountAfter > previousCount)
            {
                positive.Add(current);
            }

            previousCount = current.CountAfter;
        }

        return positive;
    }

    private static TimeSpan GetBossKillDuration(BossHistoryEntry boss)
    {
        var duration = boss.KillDuration > TimeSpan.Zero
            ? boss.KillDuration
            : boss.DefeatedAt - boss.StartedAt;

        return duration < TimeSpan.Zero ? TimeSpan.Zero : duration;
    }
}
