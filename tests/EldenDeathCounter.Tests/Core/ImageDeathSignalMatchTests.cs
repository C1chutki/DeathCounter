using EldenDeathCounter.Core.Detection;

namespace EldenDeathCounter.Tests.Core;

public sealed class ImageDeathSignalMatchTests
{
    // Mirrors the real DS3 frame #201 that was lost: a fully-gated template match that appeared on a
    // single frame before the banner faded. Such a match is specific enough to count on its own.
    private const string StrongTemplateDetails =
        "threshold=0.436; candidate=409,48,871x118; contrast=True; strokeRatio=0.952; backgroundRatio=0.002; verticalCoverage=1.000";

    [Fact]
    public void StrongGatedTemplateMatchAllowsSingleFrameConfirmation()
    {
        var signal = new ImageDeathSignalMatch(true, 0.491, "template:ENG_YouDied.jpg:opencv", 1.0)
        {
            Details = StrongTemplateDetails
        };

        Assert.True(signal.IsStrongTemplateMatch);
    }

    [Fact]
    public void OcrMatchIsNotAStrongTemplateMatch()
    {
        // OCR is noisier than a gated template, so a single OCR frame must never confirm on its own.
        var signal = new ImageDeathSignalMatch(true, 1.0, "ocr:YOU DIED", 1.0)
        {
            Details = "reason=contains-normalized-phrase; threshold=0.800"
        };

        Assert.False(signal.IsStrongTemplateMatch);
    }

    [Fact]
    public void NonMatchTemplateIsNotAStrongTemplateMatch()
    {
        var signal = new ImageDeathSignalMatch(false, 0.370, "template:ENG_YouDied.jpg:opencv", 1.0)
        {
            Details = StrongTemplateDetails
        };

        Assert.False(signal.IsStrongTemplateMatch);
    }

    [Fact]
    public void TemplateMatchWithoutStrongMetricsIsNotAStrongTemplateMatch()
    {
        // No contrast/strokeRatio/verticalCoverage detail: keep the 2-frame requirement for safety.
        var signal = new ImageDeathSignalMatch(true, 0.93, "template:NIE ZYJESZ", 1.0);

        Assert.False(signal.IsStrongTemplateMatch);
    }

    [Fact]
    public void TemplateMatchWithWeakVerticalCoverageIsNotStrong()
    {
        var signal = new ImageDeathSignalMatch(true, 0.45, "template:Death_Screen.png", 1.0)
        {
            Details = "threshold=0.436; contrast=True; strokeRatio=0.80; verticalCoverage=0.50"
        };

        Assert.False(signal.IsStrongTemplateMatch);
    }

    [Fact]
    public void PendingConfirmationUsesStructuredThresholdWhenPresent()
    {
        var signal = new ImageDeathSignalMatch(false, 0.426, "template:Death_Screen.png", 1.0)
        {
            Threshold = 0.436,
            Details = "contrast=True; strokeRatio=0.80; verticalCoverage=0.90"
        };

        Assert.True(signal.CanConfirmPendingSignal);
    }
}
