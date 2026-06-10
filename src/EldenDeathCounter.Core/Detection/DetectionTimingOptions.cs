namespace EldenDeathCounter.Core.Detection;

public static class DetectionTimingOptions
{
    public const int MinimumBaseIntervalMs = 350;
    public const int DefaultBaseIntervalMs = 350;
    public const int BurstIntervalMs = 200;
    public const int BurstDurationMs = 1500;
    public const double BurstWeakSignalFloor = 0.35;
    public const int FullDiagnosticsScreenshotIntervalMs = 1000;
    public const int FullDiagnosticsScreenshotFrameInterval = 10;

    public static int NormalizeBaseIntervalMs(int intervalMs) =>
        Math.Max(MinimumBaseIntervalMs, intervalMs);

    public static bool ShouldEnterBurst(
        ImageDeathSignalMatch imageSignal,
        bool stabilizerPending,
        ImageDeathSignalMatch selectedSignal)
    {
        return stabilizerPending ||
               selectedSignal.IsMatch ||
               imageSignal.IsMatch ||
               imageSignal.Score >= BurstWeakSignalFloor ||
               selectedSignal.Score >= BurstWeakSignalFloor;
    }

    public static bool ShouldSaveFullDiagnosticsFrame(
        DateTimeOffset frameStartedAt,
        DateTimeOffset? lastSavedAt,
        long frameIndex)
    {
        return lastSavedAt is null ||
               frameStartedAt - lastSavedAt.Value >= TimeSpan.FromMilliseconds(FullDiagnosticsScreenshotIntervalMs) ||
               frameIndex % FullDiagnosticsScreenshotFrameInterval == 0;
    }
}
