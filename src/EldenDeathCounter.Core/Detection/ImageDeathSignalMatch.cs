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
