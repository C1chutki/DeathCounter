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
            .ThenBy(item => item.KillDuration)
            .ThenBy(item => item.Name, StringComparer.CurrentCultureIgnoreCase)
            .FirstOrDefault();
        var hardest = state.BossHistory
            .OrderByDescending(item => item.DeathCount)
            .ThenByDescending(item => item.KillDuration)
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
            hardest?.Name ?? string.Empty,
            hardest?.DeathCount ?? 0,
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
}
