namespace EldenDeathCounter.Core.Storage;

public sealed class DeathCounterState
{
    public int CurrentDeathCount { get; set; }

    public List<DeathEvent> DeathEvents { get; set; } = [];

    public ActiveBossState? ActiveBoss { get; set; }

    public List<BossHistoryEntry> BossHistory { get; set; } = [];
}
