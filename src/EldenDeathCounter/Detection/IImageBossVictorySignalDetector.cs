using System.Drawing;
using EldenDeathCounter.Core.Detection;

namespace EldenDeathCounter.Detection;

public interface IImageBossVictorySignalDetector
{
    ImageDeathSignalMatch Analyze(Bitmap bitmap, double sensitivity, string gameLanguage);
}
