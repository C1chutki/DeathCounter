using System.Drawing;

namespace EldenDeathCounter.Detection;

public interface IBossNameDetector
{
    Task<string?> DetectBossNameAsync(Bitmap screenshot, CancellationToken cancellationToken);
}
