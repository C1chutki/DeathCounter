using EldenDeathCounter.Core.Logging;

namespace EldenDeathCounter.Core.Storage;

public sealed class DeathCounterService
{
    private const int MaximumStoredDeathEvents = 1_000;
    private const int DeathEventArchiveBatchSize = 250;
    private readonly DeathCounterStore _store;
    private readonly ILogService _log;
    private string _dataFilePath;
    private readonly SemaphoreSlim _saveLock = new(1, 1);

    public DeathCounterService(DeathCounterStore store, ILogService log, string dataFilePath, DeathCounterState state)
    {
        _store = store;
        _log = log;
        _dataFilePath = dataFilePath;
        State = state;
    }

    public DeathCounterState State { get; private set; }

    public event EventHandler? StateChanged;

    public async Task SwitchDataFileAsync(string dataFilePath)
    {
        await _saveLock.WaitAsync();
        try
        {
            _dataFilePath = dataFilePath;
            State = await _store.LoadAsync(_dataFilePath);
            _log.Info($"Death data switched to {_dataFilePath}.");
            StateChanged?.Invoke(this, EventArgs.Empty);
        }
        finally
        {
            _saveLock.Release();
        }
    }

    public async Task AddDeathAsync(string detectionMethod, string note)
    {
        await MutateAndPersistAsync(
            () =>
            {
                State.CurrentDeathCount++;
                if (State.ActiveBoss is not null)
                {
                    State.ActiveBoss.DeathCount++;
                }

                AddEvent(detectionMethod, note);
            },
            () => $"Death added by {detectionMethod}. Count is now {State.CurrentDeathCount}.");
    }

    public async Task SubtractDeathAsync(string detectionMethod, string note)
    {
        await MutateAndPersistAsync(
            () =>
            {
                State.CurrentDeathCount = Math.Max(0, State.CurrentDeathCount - 1);
                if (State.ActiveBoss is not null)
                {
                    State.ActiveBoss.DeathCount = Math.Max(0, State.ActiveBoss.DeathCount - 1);
                }

                AddEvent(detectionMethod, note);
            },
            () => $"Death subtracted by {detectionMethod}. Count is now {State.CurrentDeathCount}.");
    }

    public async Task ResetAsync(string detectionMethod, string note)
    {
        await MutateAndPersistAsync(
            () =>
            {
                State.CurrentDeathCount = 0;
                AddEvent(detectionMethod, note);
            },
            () => $"Counter reset by {detectionMethod}.");
    }

    public async Task SetCountAsync(int count, string detectionMethod, string note)
    {
        await MutateAndPersistAsync(
            () =>
            {
                State.CurrentDeathCount = Math.Max(0, count);
                AddEvent(detectionMethod, note);
            },
            () => $"Counter set by {detectionMethod}. Count is now {State.CurrentDeathCount}.");
    }

    public async Task SetActiveBossAsync(
        string bossName,
        bool resetIfChanged = true,
        bool timerRunning = true,
        DateTimeOffset? now = null)
    {
        var normalizedName = bossName.Trim();
        if (string.IsNullOrWhiteSpace(normalizedName))
        {
            return;
        }

        var timestamp = now ?? DateTimeOffset.Now;

        if (State.ActiveBoss is not null &&
            string.Equals(State.ActiveBoss.Name, normalizedName, StringComparison.CurrentCultureIgnoreCase))
        {
            if (timerRunning)
            {
                await ResumeActiveBossTimerAsync(timestamp);
            }
            else
            {
                await PauseActiveBossTimerAsync(timestamp);
            }

            return;
        }

        await MutateAndPersistAsync(
            () =>
            {
                if (State.ActiveBoss is not null && !resetIfChanged)
                {
                    State.ActiveBoss.Name = normalizedName;
                    return;
                }

                State.ActiveBoss = new ActiveBossState
                {
                    Name = normalizedName,
                    DeathCount = 0,
                    StartedAt = timestamp,
                    TimerStartedAt = timerRunning ? timestamp : null,
                    IsTimerRunning = timerRunning
                };
            },
            () => $"Active boss set to '{normalizedName}'.");
    }

    public Task RenameActiveBossAsync(string bossName)
    {
        return SetActiveBossAsync(bossName, resetIfChanged: false);
    }

    public async Task ClearActiveBossAsync(string detectionMethod)
    {
        await MutateAndPersistAsync(
            () => State.ActiveBoss = null,
            () => $"Active boss cleared by {detectionMethod}.");
    }

    public async Task SkipActiveBossAsync(string detectionMethod)
    {
        await MutateAndPersistAsync(
            () =>
            {
                if (State.ActiveBoss is null)
                {
                    return;
                }

                State.CurrentDeathCount = Math.Max(0, State.CurrentDeathCount - State.ActiveBoss.DeathCount);
                State.ActiveBoss = null;
            },
            () => $"Active boss skipped by {detectionMethod}.");
    }

    public async Task MarkActiveBossDefeatedAsync(string detectionMethod)
    {
        await MarkActiveBossDefeatedAsync(detectionMethod, DateTimeOffset.Now);
    }

    public async Task MarkActiveBossDefeatedAsync(string detectionMethod, DateTimeOffset defeatedAt)
    {
        await MutateAndPersistAsync(
            () =>
            {
                if (State.ActiveBoss is null)
                {
                    return;
                }

                var killDuration = State.ActiveBoss.GetElapsedDuration(defeatedAt);
                State.BossHistory.Add(new BossHistoryEntry
                {
                    Name = State.ActiveBoss.Name,
                    DeathCount = State.ActiveBoss.DeathCount,
                    StartedAt = State.ActiveBoss.StartedAt,
                    DefeatedAt = defeatedAt,
                    KillDuration = killDuration,
                    CompletedBy = detectionMethod
                });
                State.ActiveBoss = null;
            },
            () => $"Active boss marked defeated by {detectionMethod}.");
    }

    public async Task PauseActiveBossTimerAsync(DateTimeOffset? pausedAt = null)
    {
        var timestamp = pausedAt ?? DateTimeOffset.Now;
        await MutateAndPersistAsync(
            () =>
            {
                if (State.ActiveBoss is null || !State.ActiveBoss.IsTimerRunning)
                {
                    return;
                }

                State.ActiveBoss.ElapsedBeforeCurrentRun = State.ActiveBoss.GetElapsedDuration(timestamp);
                State.ActiveBoss.TimerStartedAt = null;
                State.ActiveBoss.IsTimerRunning = false;
            },
            () => "Active boss timer paused.");
    }

    public async Task ResumeActiveBossTimerAsync(DateTimeOffset? resumedAt = null)
    {
        var timestamp = resumedAt ?? DateTimeOffset.Now;
        await MutateAndPersistAsync(
            () =>
            {
                if (State.ActiveBoss is null || State.ActiveBoss.IsTimerRunning)
                {
                    return;
                }

                State.ActiveBoss.TimerStartedAt = timestamp;
                State.ActiveBoss.IsTimerRunning = true;
            },
            () => "Active boss timer resumed.");
    }

    public async Task UpdateBossHistoryEntryAsync(
        BossHistoryEntry entry,
        string name,
        int deathCount,
        DateTimeOffset startedAt,
        DateTimeOffset defeatedAt,
        string completedBy)
    {
        if (!State.BossHistory.Contains(entry))
        {
            return;
        }

        var normalizedName = name.Trim();
        if (string.IsNullOrWhiteSpace(normalizedName))
        {
            return;
        }

        await MutateAndPersistAsync(
            () =>
            {
                entry.Name = normalizedName;
                entry.DeathCount = Math.Max(0, deathCount);
                entry.StartedAt = startedAt;
                entry.DefeatedAt = defeatedAt;
                entry.KillDuration = defeatedAt >= startedAt
                    ? defeatedAt - startedAt
                    : TimeSpan.Zero;
                entry.CompletedBy = completedBy.Trim();
            },
            () => $"Boss history entry '{entry.Name}' updated.");
    }

    public async Task AddBossHistoryEntryAsync(
        string name,
        int deathCount,
        DateTimeOffset startedAt,
        DateTimeOffset defeatedAt,
        string completedBy)
    {
        var normalizedName = name.Trim();
        if (string.IsNullOrWhiteSpace(normalizedName))
        {
            return;
        }

        await MutateAndPersistAsync(
            () =>
            {
                State.BossHistory.Add(new BossHistoryEntry
                {
                    Name = normalizedName,
                    DeathCount = Math.Max(0, deathCount),
                    StartedAt = startedAt,
                    DefeatedAt = defeatedAt,
                    KillDuration = defeatedAt >= startedAt
                        ? defeatedAt - startedAt
                        : TimeSpan.Zero,
                    CompletedBy = completedBy.Trim()
                });
            },
            () => $"Boss history entry '{normalizedName}' added.");
    }

    public async Task DeleteBossHistoryEntryAsync(BossHistoryEntry entry)
    {
        if (!State.BossHistory.Contains(entry))
        {
            return;
        }

        var deletedName = entry.Name;
        await MutateAndPersistAsync(
            () => State.BossHistory.Remove(entry),
            () => $"Boss history entry '{deletedName}' deleted.");
    }

    private void AddEvent(string detectionMethod, string note)
    {
        State.DeathEvents.Add(new DeathEvent
        {
            Timestamp = DateTimeOffset.Now,
            DetectionMethod = detectionMethod,
            Note = note,
            CountAfter = State.CurrentDeathCount,
            BossName = State.ActiveBoss?.Name,
            BossDeathCountAfter = State.ActiveBoss?.DeathCount
        });
    }

    private async Task MutateAndPersistAsync(Action mutate, Func<string> createLogMessage)
    {
        await _saveLock.WaitAsync();
        try
        {
            mutate();
            await ArchiveExcessDeathEventsAsync();
            await _store.SaveAsync(_dataFilePath, State);
            _log.Info(createLogMessage());
            StateChanged?.Invoke(this, EventArgs.Empty);
        }
        finally
        {
            _saveLock.Release();
        }
    }

    private async Task ArchiveExcessDeathEventsAsync()
    {
        var excessEventCount = State.DeathEvents.Count - MaximumStoredDeathEvents;
        if (excessEventCount <= 0)
        {
            return;
        }

        var eventsToArchive = State.DeathEvents
            .OrderBy(item => item.Timestamp)
            .Take(Math.Max(excessEventCount, DeathEventArchiveBatchSize))
            .ToList();

        await _store.ArchiveDeathEventsAsync(_dataFilePath, eventsToArchive);
        State.DeathEvents.RemoveAll(item => eventsToArchive.Contains(item));
    }
}
