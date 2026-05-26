using System.Text.Json;
using EldenDeathCounter.Core.Configuration;
using EldenDeathCounter.Core.Storage;

namespace EldenDeathCounter.Core.Logging;

public sealed class DetectionEventLogService : IDetectionEventLogService
{
    private const int RecentEventLimit = 100;
    private static readonly JsonSerializerOptions JsonLineOptions = new(JsonFileOptions.Value)
    {
        WriteIndented = false
    };
    private readonly object _syncRoot = new();
    private readonly Queue<DetectionEventRecord> _recentEvents = new();
    private AppSettings? _settings;
    private DetectionDiagnosticsState _state = new(0, null, false);
    private RollingFileWriter? _writer;

    public void Configure(AppSettings settings, DetectionDiagnosticsState state)
    {
        lock (_syncRoot)
        {
            _settings = settings;
            _state = state;
            _writer = settings.DiagnosticsMode == DiagnosticsMode.Off
                ? null
                : new RollingFileWriter(
                    Path.Combine(settings.DataFolderPath, "detection-events.jsonl"),
                    settings.DiagnosticsMaxEventLogMb * 1024L * 1024L,
                    retainedFiles: 5);
            WriteLatestSnapshot();
        }
    }

    public void Log(DetectionEventRecord record)
    {
        lock (_syncRoot)
        {
            if (_settings is null)
            {
                return;
            }

            if (_settings.DiagnosticsMode == DiagnosticsMode.Off)
            {
                WriteLatestSnapshot();
                return;
            }

            if (record.IsFrameDiagnostic && _settings.DiagnosticsMode != DiagnosticsMode.FullFrames)
            {
                WriteLatestSnapshot();
                return;
            }

            record.Reason = Compact(record.Reason, 500);
            record.Evidence = NormalizeEvidencePath(record.Evidence);
            _writer ??= new RollingFileWriter(
                Path.Combine(_settings.DataFolderPath, "detection-events.jsonl"),
                _settings.DiagnosticsMaxEventLogMb * 1024L * 1024L,
                retainedFiles: 5);
            _writer.WriteLine(JsonSerializer.Serialize(record, JsonLineOptions));

            _recentEvents.Enqueue(record);
            while (_recentEvents.Count > RecentEventLimit)
            {
                _recentEvents.Dequeue();
            }

            WriteLatestSnapshot();
        }
    }

    private void WriteLatestSnapshot()
    {
        if (_settings is null)
        {
            return;
        }

        var snapshot = new
        {
            generatedAt = DateTimeOffset.Now,
            settings = new
            {
                captureTarget = _settings.CaptureTarget,
                intervalMs = _settings.DetectionIntervalMs,
                sensitivity = _settings.DetectionSensitivity,
                diagnosticsMode = _settings.DiagnosticsMode.ToString()
            },
            state = new
            {
                deathCount = _state.DeathCount,
                activeBoss = _state.ActiveBoss,
                detectionRunning = _state.DetectionRunning
            },
            recentEvents = _recentEvents.ToArray()
        };

        Directory.CreateDirectory(_settings.DataFolderPath);
        File.WriteAllText(
            Path.Combine(_settings.DataFolderPath, "diagnostics-latest.json"),
            JsonSerializer.Serialize(snapshot, JsonFileOptions.Value));
    }

    private string? NormalizeEvidencePath(string? evidence)
    {
        if (string.IsNullOrWhiteSpace(evidence) || _settings is null)
        {
            return evidence;
        }

        try
        {
            var dataFolder = Path.GetFullPath(_settings.DataFolderPath);
            var evidencePath = Path.GetFullPath(evidence);
            var relative = Path.GetRelativePath(dataFolder, evidencePath);
            return relative.StartsWith("..", StringComparison.Ordinal)
                ? evidence
                : relative.Replace('\\', '/');
        }
        catch
        {
            return evidence;
        }
    }

    private static string? Compact(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return value;
        }

        var compact = value
            .Replace("\r", " ", StringComparison.Ordinal)
            .Replace("\n", " ", StringComparison.Ordinal)
            .Trim();
        return compact.Length <= maxLength ? compact : compact[..maxLength] + "...";
    }
}
