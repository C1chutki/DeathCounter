namespace EldenDeathCounter.Core.Configuration;

public sealed record DetectionModePreset(
    string Mode,
    int IntervalMs,
    int CooldownSeconds,
    double Sensitivity);
