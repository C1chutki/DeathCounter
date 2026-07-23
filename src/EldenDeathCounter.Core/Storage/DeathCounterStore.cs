using System.Text.Json;
using System.Text;
using EldenDeathCounter.Core.Logging;

namespace EldenDeathCounter.Core.Storage;

public sealed class DeathCounterStore
{
    private readonly ILogService _log;

    public DeathCounterStore(ILogService log)
    {
        _log = log;
    }

    public async Task<DeathCounterState> LoadAsync(string dataFilePath)
    {
        try
        {
            EnsureParentDirectory(dataFilePath);
            CleanupStaleTempFiles(dataFilePath);
            if (!File.Exists(dataFilePath))
            {
                var cleanState = new DeathCounterState();
                await SaveAsync(dataFilePath, cleanState);
                _log.Info($"Created death data file at {dataFilePath}.");
                return cleanState;
            }

            await using var stream = File.OpenRead(dataFilePath);
            var state = await JsonSerializer.DeserializeAsync<DeathCounterState>(stream, JsonFileOptions.Value);
            return Normalize(state ?? new DeathCounterState());
        }
        catch (JsonException exception)
        {
            _log.Error($"Corrupt death data found at {dataFilePath}. Creating backup and clean file.", exception);
            BackupCorruptFile(dataFilePath, "deaths");
            var cleanState = new DeathCounterState();
            await SaveAsync(dataFilePath, cleanState);
            return cleanState;
        }
        catch (Exception exception)
        {
            _log.Error($"Failed to load death data from {dataFilePath}.", exception);
            throw;
        }
    }

    public async Task SaveAsync(string dataFilePath, DeathCounterState state)
    {
        try
        {
            await JsonFileWriter.WriteAtomicAsync(
                dataFilePath,
                stream => JsonSerializer.SerializeAsync(stream, state, JsonFileOptions.Value));
        }
        catch (Exception exception)
        {
            _log.Error($"Failed to save death data to {dataFilePath}.", exception);
            throw;
        }
    }

    public async Task ArchiveDeathEventsAsync(string dataFilePath, IReadOnlyList<DeathEvent> events)
    {
        if (events.Count == 0)
        {
            return;
        }

        var orderedEvents = events.OrderBy(item => item.Timestamp).ToList();
        var archiveFolder = Path.Combine(Path.GetDirectoryName(dataFilePath) ?? ".", "archives");
        var archivePath = Path.Combine(
            archiveFolder,
            $"death-events-{orderedEvents[0].Timestamp.UtcDateTime.Ticks}-{orderedEvents[^1].Timestamp.UtcDateTime.Ticks}-{orderedEvents.Count}.csv");

        try
        {
            await JsonFileWriter.WriteAtomicAsync(
                archivePath,
                async stream =>
                {
                    await using var writer = new StreamWriter(stream, new UTF8Encoding(encoderShouldEmitUTF8Identifier: true), leaveOpen: true);
                    await writer.WriteAsync(CounterExportService.CreateDeathEventsCsv(orderedEvents));
                    await writer.FlushAsync();
                });
            _log.Info($"Archived {orderedEvents.Count} death events to {archivePath}.");
        }
        catch (Exception exception)
        {
            _log.Error($"Failed to archive death events to {archivePath}.", exception);
            throw;
        }
    }

    public void CleanupStaleTempFiles(string dataFilePath)
    {
        try
        {
            var directory = Path.GetDirectoryName(dataFilePath);
            if (string.IsNullOrWhiteSpace(directory) || !Directory.Exists(directory))
            {
                return;
            }

            var fileName = Path.GetFileName(dataFilePath);
            // Only files matching this app's own atomic-write naming pattern are removed:
            //   deaths.json.<guid>.tmp  (orphaned in-flight writes)
            //   deaths.json.progression (legacy/abandoned progression temp)
            // The live data file and *.corrupt-*.json backups never match these patterns.
            var patterns = new[] { $"{fileName}.*.tmp", $"{fileName}.progression" };
            foreach (var pattern in patterns)
            {
                foreach (var file in Directory.EnumerateFiles(directory, pattern))
                {
                    if (string.Equals(Path.GetFileName(file), fileName, StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    try
                    {
                        File.Delete(file);
                        _log.Info($"Removed stale storage temp file '{file}'.");
                    }
                    catch (IOException)
                    {
                        // Likely locked by an in-flight write; leave it for next startup.
                    }
                    catch (UnauthorizedAccessException)
                    {
                    }
                }
            }
        }
        catch (Exception exception)
        {
            _log.Error("Failed to clean up stale storage temp files.", exception);
        }
    }

    private static void EnsureParentDirectory(string filePath)
    {
        var directory = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }
    }

    private static void BackupCorruptFile(string filePath, string filePrefix)
    {
        if (!File.Exists(filePath))
        {
            return;
        }

        var directory = Path.GetDirectoryName(filePath) ?? ".";
        var backupPath = Path.Combine(directory, $"{filePrefix}.corrupt-{DateTime.Now:yyyyMMdd-HHmmssfff}.json");
        File.Move(filePath, backupPath, overwrite: false);
    }

    private static DeathCounterState Normalize(DeathCounterState state)
    {
        state.CurrentDeathCount = Math.Max(0, state.CurrentDeathCount);
        state.DeathEvents ??= [];
        state.BossHistory ??= [];
        if (state.ActiveBoss is not null)
        {
            state.ActiveBoss.Name = state.ActiveBoss.Name.Trim();
            state.ActiveBoss.DeathCount = Math.Max(0, state.ActiveBoss.DeathCount);
            if (string.IsNullOrWhiteSpace(state.ActiveBoss.Name))
            {
                state.ActiveBoss = null;
            }
        }

        return state;
    }
}
