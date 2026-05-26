using EldenDeathCounter.Core.Configuration;

namespace EldenDeathCounter.Core.Logging;

public interface IDetectionEventLogService
{
    void Configure(AppSettings settings, DetectionDiagnosticsState state);

    void Log(DetectionEventRecord record);
}
