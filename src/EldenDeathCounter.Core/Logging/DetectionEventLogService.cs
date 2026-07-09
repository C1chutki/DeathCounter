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
    // The latest-snapshot file is rewritten on every Log() call, and Log() runs a few times per detection
    // frame (~3-4×/s) even when diagnostics is Off. A synchronous full-file write that often is pure CPU/IO
    // waste, so the per-frame paths are throttled to this interval; meaningful moments (Configure, a real
    // written event) force an immediate write so the file stays fresh when it matters.
    private static readonly TimeSpan SnapshotThrottle = TimeSpan.FromSeconds(1);
    private readonly object _syncRoot = new();
    private readonly Queue<DetectionEventRecord> _recentEvents = new();
    private AppSettings? _settings;
    private DetectionDiagnosticsState _state = new(0, null, false);
    private RollingFileWriter? _writer;
    private DateTimeOffset _lastSnapshotWriteAt = DateTimeOffset.MinValue;

    public bool FrameDiagnosticsEnabled => _settings?.DiagnosticsMode == DiagnosticsMode.FullFrames;

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
            WriteLatestSnapshot(force: true);
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
                WriteLatestSnapshot(force: false);
                return;
            }

            if (record.IsFrameDiagnostic && _settings.DiagnosticsMode != DiagnosticsMode.FullFrames)
            {
                WriteLatestSnapshot(force: false);
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

            WriteLatestSnapshot(force: true);
        }
    }

    // Always called under _syncRoot, so _lastSnapshotWriteAt needs no extra synchronization.
    private void WriteLatestSnapshot(bool force)
    {
        if (_settings is null)
        {
            return;
        }

        var now = DateTimeOffset.Now;
        if (!force && now - _lastSnapshotWriteAt < SnapshotThrottle)
        {
            return;
        }

        _lastSnapshotWriteAt = now;

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
