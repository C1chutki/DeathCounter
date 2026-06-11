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
    string HardestBossName,
    int HardestBossDeaths,
    IReadOnlyList<DeathEvent> RecentEvents);
