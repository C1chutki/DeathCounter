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
        Assert.Equal(TimeSpan.FromMinutes(1), summary.BestBossDuration);
        Assert.Equal("Margit", summary.HardestBossName);
        Assert.Equal(12, summary.HardestBossDeaths);
        Assert.Equal(TimeSpan.FromMinutes(25), summary.HardestBossDuration);
        Assert.Equal("Margit", summary.LongestBossName);
        Assert.Equal(12, summary.LongestBossDeaths);
        Assert.Equal(TimeSpan.FromMinutes(25), summary.LongestBossDuration);
        Assert.Equal(3, summary.RecentEvents.Count);
        Assert.Equal(DateTimeOffset.Parse("2026-06-09T11:30:00+02:00"), summary.RecentEvents[0].Timestamp);
    }

    [Fact]
    public void ChoosesBestBossByFewestDeathsThenFastestKill()
    {
        var now = DateTimeOffset.Parse("2026-06-09T12:00:00+02:00");
        var state = new DeathCounterState
        {
            BossHistory =
            [
                new() { Name = "Slow clean kill", DeathCount = 1, KillDuration = TimeSpan.FromMinutes(12), CompletedBy = "manual" },
                new() { Name = "Fast clean kill", DeathCount = 1, KillDuration = TimeSpan.FromMinutes(3), CompletedBy = "manual" },
                new() { Name = "Messy quick kill", DeathCount = 2, KillDuration = TimeSpan.FromMinutes(1), CompletedBy = "manual" },
            ]
        };

        var summary = CounterStatsService.CreateSummary(state, DateTimeOffset.Parse("2026-06-09T10:00:00+02:00"), now);

        Assert.Equal("Fast clean kill", summary.BestBossName);
        Assert.Equal(1, summary.BestBossDeaths);
        Assert.Equal(TimeSpan.FromMinutes(3), summary.BestBossDuration);
    }

    [Fact]
    public void ChoosesHardestBossByMostDeathsAndLongestBossByMostTime()
    {
        var now = DateTimeOffset.Parse("2026-06-09T12:00:00+02:00");
        var state = new DeathCounterState
        {
            BossHistory =
            [
                new() { Name = "Death wall", DeathCount = 22, KillDuration = TimeSpan.FromMinutes(18), CompletedBy = "manual" },
                new() { Name = "Long duel", DeathCount = 8, KillDuration = TimeSpan.FromMinutes(41), CompletedBy = "manual" },
                new() { Name = "Short fight", DeathCount = 2, KillDuration = TimeSpan.FromMinutes(4), CompletedBy = "manual" },
            ]
        };

        var summary = CounterStatsService.CreateSummary(state, DateTimeOffset.Parse("2026-06-09T10:00:00+02:00"), now);

        Assert.Equal("Death wall", summary.HardestBossName);
        Assert.Equal(22, summary.HardestBossDeaths);
        Assert.Equal(TimeSpan.FromMinutes(18), summary.HardestBossDuration);
        Assert.Equal("Long duel", summary.LongestBossName);
        Assert.Equal(8, summary.LongestBossDeaths);
        Assert.Equal(TimeSpan.FromMinutes(41), summary.LongestBossDuration);
    }
}
