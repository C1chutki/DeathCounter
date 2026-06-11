using EldenDeathCounter.Core.Storage;

namespace EldenDeathCounter.Tests.Core;

public sealed class CounterStatsServiceTests
{
    [Fact]
    public void CountsFirstDeathEventOnFreshProfile()
    {
        var now = DateTimeOffset.Parse("2026-06-09T12:00:00+02:00");
        var state = new DeathCounterState
        {
            CurrentDeathCount = 1,
            DeathEvents =
            [
                new() { Timestamp = DateTimeOffset.Parse("2026-06-09T11:30:00+02:00"), CountAfter = 1, DetectionMethod = "template", Note = "Detected" }
            ]
        };

        var summary = CounterStatsService.CreateSummary(state, DateTimeOffset.Parse("2026-06-09T11:00:00+02:00"), now);

        Assert.Equal(1, summary.DeathsToday);
        Assert.Equal(1, summary.SessionDeaths);
    }

    [Fact]
    public void CreatesSummaryFromDeathEventsBossHistoryAndActiveBoss()
    {
        var now = DateTimeOffset.Parse("2026-06-09T12:00:00+02:00");
        var sessionStartedAt = DateTimeOffset.Parse("2026-06-09T10:00:00+02:00");
        var state = new DeathCounterState
        {
            CurrentDeathCount = 5,
            ActiveBoss = new ActiveBossState
            {
                Name = "Malenia",
                DeathCount = 2,
                StartedAt = DateTimeOffset.Parse("2026-06-09T10:15:00+02:00")
            },
            DeathEvents =
            [
                new() { Timestamp = DateTimeOffset.Parse("2026-06-08T22:00:00+02:00"), CountAfter = 3, DetectionMethod = "manual-button", Note = "Added" },
                new() { Timestamp = DateTimeOffset.Parse("2026-06-09T09:00:00+02:00"), CountAfter = 4, DetectionMethod = "manual-button", Note = "Added" },
                new() { Timestamp = DateTimeOffset.Parse("2026-06-09T10:30:00+02:00"), CountAfter = 5, DetectionMethod = "template", Note = "Detected" },
                new() { Timestamp = DateTimeOffset.Parse("2026-06-09T11:00:00+02:00"), CountAfter = 4, DetectionMethod = "manual-button", Note = "Subtracted" },
                new() { Timestamp = DateTimeOffset.Parse("2026-06-09T11:30:00+02:00"), CountAfter = 5, DetectionMethod = "manual-button", Note = "Added" },
            ],
            BossHistory =
            [
                new() { Name = "Soldier of Godrick", DeathCount = 0, KillDuration = TimeSpan.FromMinutes(1), CompletedBy = "manual" },
                new() { Name = "Margit", DeathCount = 12, KillDuration = TimeSpan.FromMinutes(25), CompletedBy = "manual" },
                new() { Name = "Godrick", DeathCount = 4, KillDuration = TimeSpan.FromMinutes(9), CompletedBy = "manual" },
            ]
        };

        var summary = CounterStatsService.CreateSummary(state, sessionStartedAt, now);

        Assert.Equal(5, summary.TotalDeaths);
        Assert.Equal(3, summary.DeathsToday);
        Assert.Equal(2, summary.SessionDeaths);
        Assert.Equal(1.0, summary.DeathsPerHour, precision: 2);
        Assert.Equal("Malenia", summary.ActiveBossName);
        Assert.Equal(2, summary.ActiveBossDeaths);
        Assert.Equal("Soldier of Godrick", summary.BestBossName);
        Assert.Equal(0, summary.BestBossDeaths);
        Assert.Equal("Margit", summary.HardestBossName);
        Assert.Equal(12, summary.HardestBossDeaths);
        Assert.Equal(3, summary.RecentEvents.Count);
        Assert.Equal(DateTimeOffset.Parse("2026-06-09T11:30:00+02:00"), summary.RecentEvents[0].Timestamp);
    }
}
