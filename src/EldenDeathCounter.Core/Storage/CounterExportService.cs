using System.Globalization;
using System.IO.Compression;
using System.Text;

namespace EldenDeathCounter.Core.Storage;

public static class CounterExportService
{
    public static async Task<CounterExportResult> ExportAsync(
        DeathCounterState state,
        string dataFolderPath,
        string deathsFilePath,
        string settingsFilePath,
        DateTimeOffset? timestamp = null)
    {
        var exportedAt = timestamp ?? DateTimeOffset.Now;
        var exportFolder = Path.Combine(dataFolderPath, "exports");
        Directory.CreateDirectory(exportFolder);
        var stamp = exportedAt.ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture);
        var deathEventsCsv = Path.Combine(exportFolder, $"death-events-{stamp}.csv");
        var bossHistoryCsv = Path.Combine(exportFolder, $"boss-history-{stamp}.csv");
        var backupZip = Path.Combine(exportFolder, $"profile-backup-{stamp}.zip");

        await File.WriteAllTextAsync(deathEventsCsv, CreateDeathEventsCsv(state), Encoding.UTF8);
        await File.WriteAllTextAsync(bossHistoryCsv, CreateBossHistoryCsv(state), Encoding.UTF8);
        CreateBackupZip(backupZip, deathsFilePath, settingsFilePath);

        return new CounterExportResult(deathEventsCsv, bossHistoryCsv, backupZip);
    }

    private static string CreateDeathEventsCsv(DeathCounterState state)
    {
        var builder = new StringBuilder();
        builder.AppendLine("timestamp,detectionMethod,note,countAfter,bossName,bossDeathCountAfter");
        foreach (var item in state.DeathEvents.OrderBy(item => item.Timestamp))
        {
            AppendCsvRow(
                builder,
                item.Timestamp.ToString("O", CultureInfo.InvariantCulture),
                item.DetectionMethod,
                item.Note,
                item.CountAfter.ToString(CultureInfo.InvariantCulture),
                item.BossName ?? string.Empty,
                item.BossDeathCountAfter?.ToString(CultureInfo.InvariantCulture) ?? string.Empty);
        }

        return builder.ToString();
    }

    private static string CreateBossHistoryCsv(DeathCounterState state)
    {
        var builder = new StringBuilder();
        builder.AppendLine("name,deathCount,startedAt,defeatedAt,killDuration,completedBy");
        foreach (var item in state.BossHistory.OrderBy(item => item.DefeatedAt))
        {
            AppendCsvRow(
                builder,
                item.Name,
                item.DeathCount.ToString(CultureInfo.InvariantCulture),
                item.StartedAt.ToString("O", CultureInfo.InvariantCulture),
                item.DefeatedAt.ToString("O", CultureInfo.InvariantCulture),
                item.KillDuration.ToString("c", CultureInfo.InvariantCulture),
                item.CompletedBy);
        }

        return builder.ToString();
    }

    private static void AppendCsvRow(StringBuilder builder, params string[] cells)
    {
        builder.AppendLine(string.Join(",", cells.Select(EscapeCsv)));
    }

    private static string EscapeCsv(string value)
    {
        if (!value.Contains(',') && !value.Contains('"') && !value.Contains('\n') && !value.Contains('\r'))
        {
            return value;
        }

        return $"\"{value.Replace("\"", "\"\"", StringComparison.Ordinal)}\"";
    }

    private static void CreateBackupZip(string backupZip, string deathsFilePath, string settingsFilePath)
    {
        if (File.Exists(backupZip))
        {
            File.Delete(backupZip);
        }

        using var archive = ZipFile.Open(backupZip, ZipArchiveMode.Create);
        if (File.Exists(deathsFilePath))
        {
            archive.CreateEntryFromFile(deathsFilePath, "deaths.json", CompressionLevel.Optimal);
        }

        if (File.Exists(settingsFilePath))
        {
            archive.CreateEntryFromFile(settingsFilePath, "appsettings.json", CompressionLevel.Optimal);
        }
    }
}

public sealed record CounterExportResult(
    string DeathEventsCsvPath,
    string BossHistoryCsvPath,
    string ProfileBackupZipPath);
