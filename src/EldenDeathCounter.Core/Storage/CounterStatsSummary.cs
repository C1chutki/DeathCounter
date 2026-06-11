namespace EldenDeathCounter.Core.Storage;

public sealed record CounterStatsSummary(
    int TotalDeaths,
    int DeathsToday,
    int SessionDeaths,
    double DeathsPerHour,
    string ActiveBossName,
    int ActiveBossDeaths,
    string BestBossName,
    int BestBossDeaths,
    TimeSpan BestBossDuration,
    string HardestBossName,
    int HardestBossDeaths,
    TimeSpan HardestBossDuration,
    string LongestBossName,
    int LongestBossDeaths,
    TimeSpan LongestBossDuration,
    IReadOnlyList<DeathEvent> RecentEvents);
