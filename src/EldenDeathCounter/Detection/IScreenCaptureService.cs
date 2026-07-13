namespace EldenDeathCounter.Detection;

public interface IScreenCaptureService
{
    string? CaptureStatus => null;

    Task<CapturedFrame> CaptureAsync(string captureTarget, CancellationToken cancellationToken);

    Task<CapturedFrame> CaptureFullScreenAsync(string captureTarget, CancellationToken cancellationToken);

    Task<CapturedFrame> CaptureBossHealthBarAsync(string captureTarget, CancellationToken cancellationToken);
}
