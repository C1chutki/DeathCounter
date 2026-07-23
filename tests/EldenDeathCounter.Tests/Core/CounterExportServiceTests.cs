using System.IO.Compression;
using EldenDeathCounter.Core.Storage;

namespace EldenDeathCounter.Tests.Core;

public sealed class CounterExportServiceTests
{
    [Fact]
    public async Task ExportsCsvFilesAndProfileBackupZip()
    {
        var folder = Path.Combine(Path.GetTempPath(), "EldenDeathCounterTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        var deathsFile = Path.Combine(folder, "deaths.json");
        var settingsFile = Path.Combine(folder, "appsettings.json");
        await File.WriteAllTextAsync(deathsFile, """{"currentDeathCount":2}""");
        await File.WriteAllTextAsync(settingsFile, """{"detectionIntervalMs":300}""");
        var state = new DeathCounterState
        {
            CurrentDeathCount = 2,
            DeathEvents =
            [
                new()
                {
                    Timestamp = DateTimeOffset.Parse("2026-06-09T10:00:00+02:00"),
                    DetectionMethod = "manual-button",
                    Note = "Added, from test",
                    CountAfter = 2,
                    BossName = "Margit",
                    BossDeathCountAfter = 1
                }
            ],
            BossHistory =
            [
                new()
                {
                    Name = "Margit, the Fell Omen",
                    DeathCount = 2,
                    StartedAt = DateTimeOffset.Parse("2026-06-09T09:00:00+02:00"),
                    DefeatedAt = DateTimeOffset.Parse("2026-06-09T10:00:00+02:00"),
                    KillDuration = TimeSpan.FromHours(1),
                    CompletedBy = "manual-button"
                }
            ]
        };

        var result = await CounterExportService.ExportAsync(state, folder, deathsFile, settingsFile);

        Assert.True(File.Exists(result.DeathEventsCsvPath));
        Assert.True(File.Exists(result.BossHistoryCsvPath));
        Assert.True(File.Exists(result.ProfileBackupZipPath));

        var deathEventsCsv = await File.ReadAllTextAsync(result.DeathEventsCsvPath);
        Assert.Contains("timestamp,detectionMethod,note,countAfter,bossName,bossDeathCountAfter", deathEventsCsv);
        Assert.Contains("\"Added, from test\"", deathEventsCsv);

        var bossHistoryCsv = await File.ReadAllTextAsync(result.BossHistoryCsvPath);
        Assert.Contains("name,deathCount,startedAt,defeatedAt,killDuration,completedBy", bossHistoryCsv);
        Assert.Contains("\"Margit, the Fell Omen\"", bossHistoryCsv);

        using var zip = ZipFile.OpenRead(result.ProfileBackupZipPath);
        Assert.Contains(zip.Entries, entry => entry.FullName == "deaths.json");
        Assert.Contains(zip.Entries, entry => entry.FullName == "appsettings.json");
    }
}
