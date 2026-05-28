using EldenDeathCounter.Core.Logging;
using EldenDeathCounter.Core.Storage;

namespace EldenDeathCounter.Tests.Core;

public sealed class DeathCounterStoreTests
{
    [Fact]
    public async Task LoadAsyncBacksUpCorruptJsonAndCreatesCleanState()
    {
        var folder = Path.Combine(Path.GetTempPath(), "EldenDeathCounterTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        var dataFile = Path.Combine(folder, "deaths.json");
        await File.WriteAllTextAsync(dataFile, "{ not valid json");
        var logger = new InMemoryLogService();
        var store = new DeathCounterStore(logger);

        var state = await store.LoadAsync(dataFile);

        Assert.Equal(0, state.CurrentDeathCount);
        Assert.Empty(state.DeathEvents);
        Assert.True(File.Exists(dataFile));
        Assert.Single(Directory.GetFiles(folder, "deaths.corrupt-*.json"));
        Assert.Contains(logger.Messages, message => message.Contains("Corrupt death data", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task SaveAsyncPersistsCurrentCountAndEvents()
    {
        var folder = Path.Combine(Path.GetTempPath(), "EldenDeathCounterTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        var dataFile = Path.Combine(folder, "deaths.json");
        var logger = new InMemoryLogService();
        var store = new DeathCounterStore(logger);
        var state = new DeathCounterState
        {
            CurrentDeathCount = 2,
            DeathEvents =
            [
                new DeathEvent
                {
                    Timestamp = DateTimeOffset.Parse("2026-05-23T10:00:00+02:00"),
                    DetectionMethod = "manual-button",
                    Note = "Added from test",
                    CountAfter = 2
                }
            ]
        };

        await store.SaveAsync(dataFile, state);
        var loaded = await store.LoadAsync(dataFile);

        Assert.Equal(2, loaded.CurrentDeathCount);
        Assert.Single(loaded.DeathEvents);
        Assert.Equal("manual-button", loaded.DeathEvents[0].DetectionMethod);
    }

    [Fact]
    public async Task AtomicWriteAsyncKeepsExistingFileWhenWriteFails()
    {
        var folder = Path.Combine(Path.GetTempPath(), "EldenDeathCounterTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        var dataFile = Path.Combine(folder, "deaths.json");
        await File.WriteAllTextAsync(dataFile, """{"currentDeathCount":12}""");

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            JsonFileWriter.WriteAtomicAsync(
                dataFile,
                async stream =>
                {
                    await using var writer = new StreamWriter(stream);
                    await writer.WriteAsync("""{"currentDeathCount":""");
                    await writer.FlushAsync();
                    throw new InvalidOperationException("simulated write failure");
                }));

        Assert.Equal("""{"currentDeathCount":12}""", await File.ReadAllTextAsync(dataFile));
        Assert.Empty(Directory.GetFiles(folder, "*.tmp"));
    }

    [Fact]
    public async Task SaveAsyncLeavesNoTempFilesAfterSuccess()
    {
        var folder = Path.Combine(Path.GetTempPath(), "EldenDeathCounterTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        var dataFile = Path.Combine(folder, "deaths.json");
        var store = new DeathCounterStore(new InMemoryLogService());
        var state = new DeathCounterState { CurrentDeathCount = 5 };

        await store.SaveAsync(dataFile, state);
        await store.SaveAsync(dataFile, state);

        Assert.True(File.Exists(dataFile));
        Assert.Empty(Directory.GetFiles(folder, "deaths.json.*.tmp"));
        Assert.Empty(Directory.GetFiles(folder, "*.tmp"));
    }

    [Fact]
    public void CleanupStaleTempFilesRemovesAppTempAndProgressionFiles()
    {
        var folder = Path.Combine(Path.GetTempPath(), "EldenDeathCounterTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        var dataFile = Path.Combine(folder, "deaths.json");
        var staleTemp = Path.Combine(folder, $"deaths.json.{Guid.NewGuid():N}.tmp");
        var progression = Path.Combine(folder, "deaths.json.progression");
        File.WriteAllText(staleTemp, "partial");
        File.WriteAllText(progression, "old-progression");
        var store = new DeathCounterStore(new InMemoryLogService());

        store.CleanupStaleTempFiles(dataFile);

        Assert.False(File.Exists(staleTemp));
        Assert.False(File.Exists(progression));
    }

    [Fact]
    public void CleanupStaleTempFilesPreservesDataFileAndCorruptBackups()
    {
        var folder = Path.Combine(Path.GetTempPath(), "EldenDeathCounterTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        var dataFile = Path.Combine(folder, "deaths.json");
        var backup = Path.Combine(folder, "deaths.corrupt-20260101-101010101.json");
        var unrelated = Path.Combine(folder, "notes.txt");
        File.WriteAllText(dataFile, """{"currentDeathCount":7}""");
        File.WriteAllText(backup, """{"currentDeathCount":3}""");
        File.WriteAllText(unrelated, "user notes");
        var store = new DeathCounterStore(new InMemoryLogService());

        store.CleanupStaleTempFiles(dataFile);

        Assert.True(File.Exists(dataFile));
        Assert.True(File.Exists(backup));
        Assert.True(File.Exists(unrelated));
    }

    [Fact]
    public async Task LoadAsyncRemovesStaleTempFilesButKeepsValidData()
    {
        var folder = Path.Combine(Path.GetTempPath(), "EldenDeathCounterTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        var dataFile = Path.Combine(folder, "deaths.json");
        await File.WriteAllTextAsync(dataFile, """{"currentDeathCount":9}""");
        var staleTemp = Path.Combine(folder, $"deaths.json.{Guid.NewGuid():N}.tmp");
        var progression = Path.Combine(folder, "deaths.json.progression");
        await File.WriteAllTextAsync(staleTemp, "partial");
        await File.WriteAllTextAsync(progression, "old-progression");
        var store = new DeathCounterStore(new InMemoryLogService());

        var state = await store.LoadAsync(dataFile);

        Assert.Equal(9, state.CurrentDeathCount);
        Assert.True(File.Exists(dataFile));
        Assert.False(File.Exists(staleTemp));
        Assert.False(File.Exists(progression));
    }

    private sealed class InMemoryLogService : ILogService
    {
        public List<string> Messages { get; } = [];

        public void Info(string message) => Messages.Add(message);

        public void Error(string message, Exception? exception = null) => Messages.Add($"{message} {exception?.GetType().Name}");
    }
}
