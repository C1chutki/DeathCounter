using EldenDeathCounter.Core.Logging;
using EldenDeathCounter.Core.Storage;

namespace EldenDeathCounter.Tests.Core;

public sealed class DeathCounterServiceBossTests
{
    [Fact]
    public async Task AddDeathIncrementsActiveBossCounterAndTagsEvent()
    {
        var (service, _) = CreateService();

        await service.SetActiveBossAsync("Black Knife Assassin");
        await service.AddDeathAsync("screen-template", "Detected death.");

        Assert.Equal(1, service.State.CurrentDeathCount);
        Assert.NotNull(service.State.ActiveBoss);
        Assert.Equal("Black Knife Assassin", service.State.ActiveBoss.Name);
        Assert.Equal(1, service.State.ActiveBoss.DeathCount);
        Assert.Equal("Black Knife Assassin", service.State.DeathEvents.Last().BossName);
        Assert.Equal(1, service.State.DeathEvents.Last().BossDeathCountAfter);
    }

    [Fact]
    public async Task SubtractDeathAlsoCorrectsActiveBossCounter()
    {
        var (service, _) = CreateService();

        await service.SetActiveBossAsync("Margit");
        await service.AddDeathAsync("manual-button", "Added.");
        await service.SubtractDeathAsync("manual-button", "Correction.");

        Assert.Equal(0, service.State.CurrentDeathCount);
        Assert.NotNull(service.State.ActiveBoss);
        Assert.Equal(0, service.State.ActiveBoss.DeathCount);
        Assert.Equal(0, service.State.DeathEvents.Last().BossDeathCountAfter);
    }

    [Fact]
    public async Task MarkActiveBossDefeatedMovesBossToHistoryAndClearsOverlayBoss()
    {
        var (service, dataFile) = CreateService();
        var startedAt = DateTimeOffset.Now.AddSeconds(-75);

        await service.SetActiveBossAsync("Tree Sentinel", now: startedAt);
        Assert.NotNull(service.State.ActiveBoss);
        await service.AddDeathAsync("manual-hotkey", "Added.");
        await service.MarkActiveBossDefeatedAsync("manual-hotkey");

        Assert.Null(service.State.ActiveBoss);
        var entry = Assert.Single(service.State.BossHistory);
        Assert.Equal("Tree Sentinel", entry.Name);
        Assert.Equal(1, entry.DeathCount);
        Assert.Equal("manual-hotkey", entry.CompletedBy);
        Assert.Equal(entry.DefeatedAt - entry.StartedAt, entry.KillDuration);
        Assert.True(entry.KillDuration >= TimeSpan.FromSeconds(75));

        var loaded = await new DeathCounterStore(new InMemoryLogService()).LoadAsync(dataFile);
        Assert.Null(loaded.ActiveBoss);
        var loadedEntry = Assert.Single(loaded.BossHistory);
        Assert.Equal(entry.KillDuration, loadedEntry.KillDuration);
    }

    [Fact]
    public async Task PauseActiveBossTimerPersistsElapsedDurationAndStopsGrowth()
    {
        var (service, dataFile) = CreateService();
        var startedAt = new DateTimeOffset(2026, 5, 25, 18, 0, 0, TimeSpan.FromHours(2));
        var pausedAt = startedAt.AddMinutes(7);
        var later = pausedAt.AddMinutes(30);

        await service.SetActiveBossAsync("Margit", timerRunning: true, now: startedAt);
        await service.PauseActiveBossTimerAsync(pausedAt);

        Assert.NotNull(service.State.ActiveBoss);
        Assert.False(service.State.ActiveBoss.IsTimerRunning);
        Assert.Equal(TimeSpan.FromMinutes(7), service.State.ActiveBoss.GetElapsedDuration(later));

        var loaded = await new DeathCounterStore(new InMemoryLogService()).LoadAsync(dataFile);
        Assert.NotNull(loaded.ActiveBoss);
        Assert.False(loaded.ActiveBoss.IsTimerRunning);
        Assert.Equal(TimeSpan.FromMinutes(7), loaded.ActiveBoss.GetElapsedDuration(later));
    }

    [Fact]
    public async Task ResumeActiveBossTimerOnlyAddsTimeAfterResume()
    {
        var (service, _) = CreateService();
        var startedAt = new DateTimeOffset(2026, 5, 25, 18, 0, 0, TimeSpan.FromHours(2));
        var pausedAt = startedAt.AddMinutes(5);
        var resumedAt = pausedAt.AddHours(1);
        var later = resumedAt.AddMinutes(4);

        await service.SetActiveBossAsync("Godrick", timerRunning: true, now: startedAt);
        await service.PauseActiveBossTimerAsync(pausedAt);
        await service.ResumeActiveBossTimerAsync(resumedAt);

        Assert.NotNull(service.State.ActiveBoss);
        Assert.True(service.State.ActiveBoss.IsTimerRunning);
        Assert.Equal(TimeSpan.FromMinutes(9), service.State.ActiveBoss.GetElapsedDuration(later));
    }

    [Fact]
    public async Task MarkActiveBossDefeatedUsesPausedAwareKillDuration()
    {
        var (service, _) = CreateService();
        var startedAt = new DateTimeOffset(2026, 5, 25, 18, 0, 0, TimeSpan.FromHours(2));
        var pausedAt = startedAt.AddMinutes(6);
        var defeatedAt = pausedAt.AddHours(2);

        await service.SetActiveBossAsync("Rennala", timerRunning: true, now: startedAt);
        await service.PauseActiveBossTimerAsync(pausedAt);
        await service.MarkActiveBossDefeatedAsync("manual-hotkey", defeatedAt);

        var entry = Assert.Single(service.State.BossHistory);
        Assert.Equal(startedAt, entry.StartedAt);
        Assert.Equal(defeatedAt, entry.DefeatedAt);
        Assert.Equal(TimeSpan.FromMinutes(6), entry.KillDuration);
    }

    [Fact]
    public async Task UpdateBossHistoryEntryPersistsEditedValuesAndRecalculatesDuration()
    {
        var (service, dataFile) = CreateService();
        var startedAt = new DateTimeOffset(2026, 5, 24, 19, 0, 0, TimeSpan.FromHours(2));
        var defeatedAt = startedAt.AddMinutes(4).AddSeconds(12);
        var entry = new BossHistoryEntry
        {
            Name = "Tree Sentinel",
            DeathCount = 1,
            StartedAt = startedAt,
            DefeatedAt = defeatedAt,
            KillDuration = defeatedAt - startedAt,
            CompletedBy = "manual-hotkey"
        };
        service.State.BossHistory.Add(entry);

        var editedStartedAt = startedAt.AddMinutes(1);
        var editedDefeatedAt = defeatedAt.AddMinutes(2);
        await service.UpdateBossHistoryEntryAsync(
            entry,
            "Margit",
            3,
            editedStartedAt,
            editedDefeatedAt,
            "manual-edit");

        Assert.Equal("Margit", entry.Name);
        Assert.Equal(3, entry.DeathCount);
        Assert.Equal(editedStartedAt, entry.StartedAt);
        Assert.Equal(editedDefeatedAt, entry.DefeatedAt);
        Assert.Equal(editedDefeatedAt - editedStartedAt, entry.KillDuration);
        Assert.Equal("manual-edit", entry.CompletedBy);

        var loaded = await new DeathCounterStore(new InMemoryLogService()).LoadAsync(dataFile);
        var loadedEntry = Assert.Single(loaded.BossHistory);
        Assert.Equal("Margit", loadedEntry.Name);
        Assert.Equal(3, loadedEntry.DeathCount);
        Assert.Equal(editedDefeatedAt - editedStartedAt, loadedEntry.KillDuration);
        Assert.Equal("manual-edit", loadedEntry.CompletedBy);
    }

    [Fact]
    public async Task DeleteBossHistoryEntryRemovesRecordAndPersistsState()
    {
        var (service, dataFile) = CreateService();
        var firstEntry = new BossHistoryEntry
        {
            Name = "Tree Sentinel",
            DeathCount = 1,
            StartedAt = DateTimeOffset.Now.AddMinutes(-5),
            DefeatedAt = DateTimeOffset.Now,
            KillDuration = TimeSpan.FromMinutes(5),
            CompletedBy = "manual-hotkey"
        };
        var secondEntry = new BossHistoryEntry
        {
            Name = "Margit",
            DeathCount = 3,
            StartedAt = DateTimeOffset.Now.AddMinutes(-8),
            DefeatedAt = DateTimeOffset.Now,
            KillDuration = TimeSpan.FromMinutes(8),
            CompletedBy = "manual-edit"
        };
        service.State.BossHistory.Add(firstEntry);
        service.State.BossHistory.Add(secondEntry);

        await service.DeleteBossHistoryEntryAsync(firstEntry);

        var remainingEntry = Assert.Single(service.State.BossHistory);
        Assert.Same(secondEntry, remainingEntry);

        var loaded = await new DeathCounterStore(new InMemoryLogService()).LoadAsync(dataFile);
        var loadedEntry = Assert.Single(loaded.BossHistory);
        Assert.Equal("Margit", loadedEntry.Name);
    }

    [Fact]
    public async Task AutoDetectedBossRenamePreservesCurrentBossDeathCount()
    {
        var (service, _) = CreateService();

        await service.SetActiveBossAsync("Boss One");
        await service.AddDeathAsync("screen-template", "Detected.");
        await service.SetActiveBossAsync("Boss One + Boss Two", resetIfChanged: false);

        Assert.NotNull(service.State.ActiveBoss);
        Assert.Equal("Boss One + Boss Two", service.State.ActiveBoss.Name);
        Assert.Equal(1, service.State.ActiveBoss.DeathCount);
    }

    [Fact]
    public async Task RenameActiveBossPreservesCurrentBossDeathCount()
    {
        var (service, _) = CreateService();

        await service.SetActiveBossAsync("giowy poieracz bog6w");
        await service.AddDeathAsync("manual-button", "Added.");
        await service.RenameActiveBossAsync("Głowy Pożeracz Bogów");

        Assert.NotNull(service.State.ActiveBoss);
        Assert.Equal("Głowy Pożeracz Bogów", service.State.ActiveBoss.Name);
        Assert.Equal(1, service.State.ActiveBoss.DeathCount);
    }

    [Fact]
    public async Task SwitchDataFileAsyncLoadsSelectedFileAndPersistsFutureChangesThere()
    {
        var logger = new InMemoryLogService();
        var folder = Path.Combine(Path.GetTempPath(), "EldenDeathCounterTests", Guid.NewGuid().ToString("N"));
        var eldenRingDataFile = Path.Combine(folder, "DeathCounter", "EldenRing", "deaths.json");
        var darkSouls3DataFile = Path.Combine(folder, "DeathCounter", "DarkSouls3", "deaths.json");
        var store = new DeathCounterStore(logger);
        await store.SaveAsync(eldenRingDataFile, new DeathCounterState { CurrentDeathCount = 8 });
        await store.SaveAsync(darkSouls3DataFile, new DeathCounterState { CurrentDeathCount = 3 });
        var service = new DeathCounterService(store, logger, eldenRingDataFile, await store.LoadAsync(eldenRingDataFile));
        var stateChanges = 0;
        service.StateChanged += (_, _) => stateChanges++;

        await service.SwitchDataFileAsync(darkSouls3DataFile);
        await service.AddDeathAsync("manual-button", "Added after switching to DS3.");

        Assert.Equal(4, service.State.CurrentDeathCount);
        Assert.Equal(2, stateChanges);
        Assert.Equal(8, (await store.LoadAsync(eldenRingDataFile)).CurrentDeathCount);
        Assert.Equal(4, (await store.LoadAsync(darkSouls3DataFile)).CurrentDeathCount);
    }

    private static (DeathCounterService Service, string DataFile) CreateService()
    {
        var folder = Path.Combine(Path.GetTempPath(), "EldenDeathCounterTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        var dataFile = Path.Combine(folder, "deaths.json");
        var logger = new InMemoryLogService();
        var store = new DeathCounterStore(logger);
        var service = new DeathCounterService(store, logger, dataFile, new DeathCounterState());
        return (service, dataFile);
    }

    private sealed class InMemoryLogService : ILogService
    {
        public void Info(string message)
        {
        }

        public void Error(string message, Exception? exception = null)
        {
        }
    }
}
