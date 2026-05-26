namespace EldenDeathCounter.Detection;

public sealed class DetectionStatusChangedEventArgs : EventArgs
{
    public DetectionStatusChangedEventArgs(string status, DateTimeOffset? lastDetectedDeath)
    {
        Status = status;
        LastDetectedDeath = lastDetectedDeath;
    }

    public string Status { get; }

    public DateTimeOffset? LastDetectedDeath { get; }
}
