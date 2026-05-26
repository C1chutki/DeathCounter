using System.Drawing;

namespace EldenDeathCounter.Detection;

public interface ITextRecognitionService
{
    Task<string> RecognizeTextAsync(Bitmap bitmap, CancellationToken cancellationToken);
}
