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

    private sealed class InMemoryLogService : ILogService
    {
        public List<string> Messages { get; } = [];

        public void Info(string message) => Messages.Add(message);

        public void Error(string message, Exception? exception = null) => Messages.Add($"{message} {exception?.GetType().Name}");
    }
}
