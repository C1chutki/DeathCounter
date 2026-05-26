using EldenDeathCounter.Core.Logging;

namespace EldenDeathCounter.Tests.Core;

public sealed class RollingFileWriterTests
{
    [Fact]
    public void WriteLineRotatesFileWhenSizeLimitWouldBeExceeded()
    {
        var folder = Path.Combine(Path.GetTempPath(), "EldenDeathCounterTests", Guid.NewGuid().ToString("N"));
        var path = Path.Combine(folder, "log.txt");
        var writer = new RollingFileWriter(path, maxBytes: 24, retainedFiles: 2);

        writer.WriteLine("first-entry");
        writer.WriteLine("second-entry-that-rotates");

        Assert.Contains("second-entry-that-rotates", File.ReadAllText(path));
        Assert.Contains("first-entry", File.ReadAllText(path + ".1"));
    }

    [Fact]
    public void WriteLineDeletesArchivesBeyondRetentionLimit()
    {
        var folder = Path.Combine(Path.GetTempPath(), "EldenDeathCounterTests", Guid.NewGuid().ToString("N"));
        var path = Path.Combine(folder, "detection-events.jsonl");
        var writer = new RollingFileWriter(path, maxBytes: 16, retainedFiles: 2);

        writer.WriteLine("entry-one");
        writer.WriteLine("entry-two");
        writer.WriteLine("entry-three");
        writer.WriteLine("entry-four");

        Assert.True(File.Exists(path));
        Assert.True(File.Exists(path + ".1"));
        Assert.True(File.Exists(path + ".2"));
        Assert.False(File.Exists(path + ".3"));
    }
}
