namespace EldenDeathCounter.Detection;

public interface IScreenCaptureService
{
    string? CaptureStatus => null;

    Task<CapturedFrame> CaptureAsync(string captureTarget, CancellationToken cancellationToken);

    // Death-text capture ROI depends on the game (Dark Souls II's "YOU DIED" sits much lower). The
    // default keeps the game-agnostic ROI so existing fakes/implementations need no change.
    Task<CapturedFrame> CaptureAsync(string captureTarget, string? gameId, CancellationToken cancellationToken) =>
        CaptureAsync(captureTarget, cancellationToken);

    Task<CapturedFrame> CaptureFullScreenAsync(string captureTarget, CancellationToken cancellationToken);

    Task<CapturedFrame> CaptureBossHealthBarAsync(string captureTarget, CancellationToken cancellationToken);
}
