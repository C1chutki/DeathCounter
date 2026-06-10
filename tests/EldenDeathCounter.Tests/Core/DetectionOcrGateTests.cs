using EldenDeathCounter.Core.Detection;

namespace EldenDeathCounter.Tests.Core;

public sealed class DetectionOcrGateTests
{
    [Fact]
    public void AllowsOcrOnlyWhenImageSignalIsWeakOrStabilizerIsPending()
    {
        var noImageSignal = ImageDeathSignalMatch.NoMatch;
        var weakImageSignal = new ImageDeathSignalMatch(false, 0.30, "template:ENG_Death_Screen.png:opencv", 1.0);

        Assert.False(DetectionOcrGate.ShouldRunOcr(noImageSignal, stabilizerPending: false));
        Assert.True(DetectionOcrGate.ShouldRunOcr(weakImageSignal, stabilizerPending: false));
        Assert.True(DetectionOcrGate.ShouldRunOcr(noImageSignal, stabilizerPending: true));
    }

    [Fact]
    public void RejectsOcrPhraseWithoutImageEvidence()
    {
        var noImageSignal = ImageDeathSignalMatch.NoMatch;
        var weakImageSignal = new ImageDeathSignalMatch(false, 0.30, "template:ENG_Death_Screen.png:opencv", 1.0);

        Assert.False(DetectionOcrGate.ShouldAcceptOcrPhrase(isPhraseMatch: true, noImageSignal, stabilizerPending: false));
        Assert.True(DetectionOcrGate.ShouldAcceptOcrPhrase(isPhraseMatch: true, weakImageSignal, stabilizerPending: false));
        Assert.True(DetectionOcrGate.ShouldAcceptOcrPhrase(isPhraseMatch: true, noImageSignal, stabilizerPending: true));
        Assert.False(DetectionOcrGate.ShouldAcceptOcrPhrase(isPhraseMatch: false, weakImageSignal, stabilizerPending: true));
    }
}
