using System.Drawing;
using EldenDeathCounter.Core.Detection;

namespace EldenDeathCounter.Detection;

public interface IImageDeathSignalDetector
{
    ImageDeathSignalMatch Analyze(Bitmap bitmap, double sensitivity, string gameLanguage);
}
