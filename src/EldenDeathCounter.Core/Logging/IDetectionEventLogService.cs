using EldenDeathCounter.Core.Configuration;

namespace EldenDeathCounter.Core.Logging;

public interface IDetectionEventLogService
{
    // True only when full per-frame diagnostics are being recorded. Callers use it to skip building the
    // expensive per-frame diagnostic strings that Log() would otherwise discard.
    bool FrameDiagnosticsEnabled { get; }

    void Configure(AppSettings settings, DetectionDiagnosticsState state);

    void Log(DetectionEventRecord record);
}
