using System.Globalization;

namespace EldenDeathCounter.Core.Detection;

public sealed record ImageDeathSignalMatch(
    bool IsMatch,
    double Score,
    string Method,
    double Scale)
{
    private const double PendingConfirmationTolerance = 0.02;

    public string Details { get; init; } = string.Empty;

    public bool CanConfirmPendingSignal =>
        !IsMatch &&
        Score > 0 &&
        Method.StartsWith("template:", StringComparison.Ordinal) &&
        HasStrongTemplateContrast() &&
        TryGetDetailScore("threshold", out var threshold) &&
        Score >= threshold - PendingConfirmationTolerance;

    // A confirmed template match whose shape metrics are strong enough to count on a single frame.
    // Fast-fading death banners (notably Dark Souls 3 at a 500ms sample rate) are often template-
    // detectable on only one frame, so a fully-gated match — passing the analyzer threshold plus
    // strong contrast, stroke coverage, and full vertical coverage — is specific enough to confirm
    // without waiting for a second consecutive frame. OCR matches are deliberately excluded (noisier).
    public bool IsStrongTemplateMatch =>
        IsMatch &&
        Method.StartsWith("template:", StringComparison.Ordinal) &&
        HasStrongTemplateContrast() &&
        TryGetDetailScore("strokeRatio", out var strokeRatio) && strokeRatio >= 0.75 &&
        TryGetDetailScore("verticalCoverage", out var verticalCoverage) && verticalCoverage >= 0.85;

    public static readonly ImageDeathSignalMatch NoMatch = new(false, 0, string.Empty, 0);

    private bool HasStrongTemplateContrast()
    {
        return Details
            .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Any(part => part.Equals("contrast=True", StringComparison.OrdinalIgnoreCase));
    }

    private bool TryGetDetailScore(string key, out double value)
    {
        foreach (var part in Details.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var pair = part.Split('=', 2, StringSplitOptions.TrimEntries);
            if (pair.Length == 2 &&
                pair[0].Equals(key, StringComparison.OrdinalIgnoreCase) &&
                double.TryParse(pair[1], NumberStyles.Float, CultureInfo.InvariantCulture, out value))
            {
                return true;
            }
        }

        value = 0;
        return false;
    }
}
