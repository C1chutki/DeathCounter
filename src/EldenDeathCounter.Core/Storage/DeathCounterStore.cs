using System.Text.Json;
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
