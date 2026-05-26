namespace EldenDeathCounter.Core.Detection;

public enum DeathDetectionDecision
{
    NoSignal,
    Count,
    IgnoreActiveScreen,
    IgnoreCooldown,
    Rearmed
}
