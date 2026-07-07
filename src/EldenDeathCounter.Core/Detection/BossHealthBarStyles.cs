namespace EldenDeathCounter.Core.Detection;

public static class BossHealthBarStyles
{
    public const string Vanilla = "Vanilla";

    public const string Reforged = "Reforged";

    public const string Convergence = "Convergence";

    public static string Normalize(string? style)
    {
        return style?.Trim() switch
        {
            { } value when value.Equals(Reforged, StringComparison.OrdinalIgnoreCase) => Reforged,
            { } value when value.Equals(Convergence, StringComparison.OrdinalIgnoreCase) => Convergence,
            _ => Vanilla
        };
    }
}
