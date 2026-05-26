using EldenDeathCounter.Core.Logging;

namespace EldenDeathCounter.Tests.Core;

public sealed class FileLogServiceTests
{
    [Fact]
    public void InfoPublishesLogEntryForDetectionLog()
    {
        var logFile = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"), "log.txt");
        var logger = new FileLogService(logFile);
        var entries = new List<LogEntry>();
        logger.EntryWritten += (_, entry) => entries.Add(entry);

        logger.Info("Detected death. Matched death signal.");

        var entry = Assert.Single(entries);
        Assert.Equal("INFO", entry.Level);
        Assert.Equal("Detected death. Matched death signal.", entry.Message);
        Assert.True(entry.IsEvent);
        Assert.Contains("[INFO]", entry.DisplayText);
        Assert.Contains("Detected death. Matched death signal.", entry.DisplayText);
        Assert.True(entry.Timestamp <= DateTimeOffset.Now);
        Assert.Contains("Detected death. Matched death signal.", File.ReadAllText(logFile));
    }

    [Fact]
    public void SwitchableLogServiceWritesFutureEntriesToSelectedLogFile()
    {
        var folder = Path.Combine(Path.GetTempPath(), "EldenDeathCounterTests", Guid.NewGuid().ToString("N"));
        var eldenRingLogFile = Path.Combine(folder, "DeathCounter", "EldenRing", "log.txt");
        var darkSouls3LogFile = Path.Combine(folder, "DeathCounter", "DarkSouls3", "log.txt");
        var logger = new SwitchableLogService(eldenRingLogFile);
        var entries = new List<LogEntry>();
        logger.EntryWritten += (_, entry) => entries.Add(entry);

        logger.Info("Elden Ring log entry.");
        logger.SwitchTo(darkSouls3LogFile);
        logger.Info("Dark Souls 3 log entry.");

        Assert.Contains("Elden Ring log entry.", File.ReadAllText(eldenRingLogFile));
        Assert.DoesNotContain("Dark Souls 3 log entry.", File.ReadAllText(eldenRingLogFile));
        Assert.Contains("Dark Souls 3 log entry.", File.ReadAllText(darkSouls3LogFile));
        Assert.Equal(["Elden Ring log entry.", "Dark Souls 3 log entry."], entries.Select(entry => entry.Message));
    }
}
