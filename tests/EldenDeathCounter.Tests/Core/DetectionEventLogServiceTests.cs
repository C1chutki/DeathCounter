using System.Text.Json;
using EldenDeathCounter.Core.Configuration;
using EldenDeathCounter.Core.Logging;

namespace EldenDeathCounter.Tests.Core;

public sealed class DetectionEventLogServiceTests
{
    [Fact]
    public void LogWritesJsonLineAndLatestSnapshot()
    {
        var folder = Path.Combine(Path.GetTempPath(), "EldenDeathCounterTests", Guid.NewGuid().ToString("N"));
        var settings = AppSettings.CreateDefault(@"C:\Users\TestUser\Desktop");
        settings.DataFolderPath = folder;
        settings.DiagnosticsMode = DiagnosticsMode.Events;
        var service = new DetectionEventLogService();

        service.Configure(settings, new DetectionDiagnosticsState(20, "Red Wolf of Radagon", true));
        service.Log(new DetectionEventRecord
        {
            Time = new DateTimeOffset(2026, 5, 25, 16, 2, 23, 123, TimeSpan.FromHours(2)),
            Frame = 2074,
            Kind = "boss-victory",
            Outcome = "pending-expired",
            ActiveBoss = "Red Wolf of Radagon",
            SelectedSignal = "boss-victory-ocr:GREAT ENEMY FELLED",
            OcrScore = 0.889,
            ImageScore = 0.218,
            Stabilizer = "1/2",
            Gate = "latched=false,clearFrames=0/3",
            Reason = "own-settings-window-text",
            Evidence = "diagnostics/evidence/summary.json",
            FrameDeltaMs = 351,
            TimingMode = "base"
        });

        var eventLogPath = Path.Combine(folder, "detection-events.jsonl");
        var latestPath = Path.Combine(folder, "diagnostics-latest.json");
        var line = Assert.Single(File.ReadAllLines(eventLogPath));
        using var eventJson = JsonDocument.Parse(line);
        Assert.Equal("boss-victory", eventJson.RootElement.GetProperty("kind").GetString());
        Assert.Equal("pending-expired", eventJson.RootElement.GetProperty("outcome").GetString());
        Assert.Equal(0.889, eventJson.RootElement.GetProperty("ocrScore").GetDouble(), precision: 3);
        Assert.Equal(351, eventJson.RootElement.GetProperty("frameDeltaMs").GetInt64());
        Assert.Equal("base", eventJson.RootElement.GetProperty("timingMode").GetString());

        using var latestJson = JsonDocument.Parse(File.ReadAllText(latestPath));
        Assert.Equal(20, latestJson.RootElement.GetProperty("state").GetProperty("deathCount").GetInt32());
        Assert.Equal("Red Wolf of Radagon", latestJson.RootElement.GetProperty("state").GetProperty("activeBoss").GetString());
        Assert.Single(latestJson.RootElement.GetProperty("recentEvents").EnumerateArray());
    }

    [Fact]
    public void LogSkipsFrameEventsUnlessFullFramesModeIsEnabled()
    {
        var folder = Path.Combine(Path.GetTempPath(), "EldenDeathCounterTests", Guid.NewGuid().ToString("N"));
        var settings = AppSettings.CreateDefault(@"C:\Users\TestUser\Desktop");
        settings.DataFolderPath = folder;
        settings.DiagnosticsMode = DiagnosticsMode.Events;
        var service = new DetectionEventLogService();

        service.Configure(settings, new DetectionDiagnosticsState(0, null, true));
        service.Log(new DetectionEventRecord
        {
            Time = DateTimeOffset.Now,
            Frame = 1,
            Kind = "death",
            Outcome = "frame",
            Reason = new string('x', 1200),
            IsFrameDiagnostic = true
        });

        Assert.False(File.Exists(Path.Combine(folder, "detection-events.jsonl")));
        Assert.True(File.Exists(Path.Combine(folder, "diagnostics-latest.json")));
    }
}
