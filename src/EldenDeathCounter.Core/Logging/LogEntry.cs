using System.Globalization;

namespace EldenDeathCounter.Core.Logging;

public sealed class LogEntry
{
    public LogEntry(DateTimeOffset timestamp, string level, string message)
    {
        Timestamp = timestamp;
        Level = level;
        Message = message;
    }

    public DateTimeOffset Timestamp { get; }

    public string Level { get; }

    public string Message { get; }

    public bool IsEvent =>
        Message.Contains("Detected death", StringComparison.OrdinalIgnoreCase) ||
        Message.Contains("Death signal confirmed", StringComparison.OrdinalIgnoreCase) ||
        Message.Contains("MATCH CONFIRMED", StringComparison.OrdinalIgnoreCase);

    public string DisplayText => $"{Timestamp.ToLocalTime().ToString("HH:mm:ss.f", CultureInfo.CurrentCulture)}  [{Level}]   {Message}";
}
