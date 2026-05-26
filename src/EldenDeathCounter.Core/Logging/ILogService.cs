namespace EldenDeathCounter.Core.Logging;

public interface ILogService
{
    void Info(string message);

    void Error(string message, Exception? exception = null);
}

public interface ILogEntrySource
{
    event EventHandler<LogEntry>? EntryWritten;
}
