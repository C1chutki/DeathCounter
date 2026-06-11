namespace EldenDeathCounter.Core.Configuration;

public static class DetectionModePresets
{
    public const string Conservative = "Conservative";
    public const string Balanced = "Balanced";
    public const string Aggressive = "Aggressive";

    private static readonly DetectionModePreset ConservativePreset = new(Conservative, 500, 35, 0.9);
    private static readonly DetectionModePreset BalancedPreset = new(Balanced, 350, 25, 0.8);
    private static readonly DetectionModePreset AggressivePreset = new(Aggressive, 250, 10, 0.65);

    public static IReadOnlyList<DetectionModePreset> All { get; } =
    [
        ConservativePreset,
        BalancedPreset,
        AggressivePreset
    ];

    public static DetectionModePreset Get(string? mode)
    {
        return All.FirstOrDefault(preset => preset.Mode.Equals(mode, StringComparison.OrdinalIgnoreCase)) ?? BalancedPreset;
    }
}
