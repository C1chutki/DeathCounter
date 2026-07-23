namespace EldenDeathCounter.Core.Detection;

public static class DetectionOcrGate
{
    public const double WeakImageSignalFloor = 0.30;

    public static bool ShouldRunOcr(ImageDeathSignalMatch imageSignal, bool stabilizerPending) =>
        stabilizerPending || imageSignal.Score >= WeakImageSignalFloor;

    public static bool ShouldAcceptOcrPhrase(
        bool isPhraseMatch,
        ImageDeathSignalMatch imageSignal,
        bool stabilizerPending)
    {
        return isPhraseMatch && ShouldRunOcr(imageSignal, stabilizerPending);
    }
}
