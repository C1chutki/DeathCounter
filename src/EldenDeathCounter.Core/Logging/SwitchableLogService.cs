namespace EldenDeathCounter.Core.Logging;

public sealed class SwitchableLogService : ILogService, ILogEntrySource
{
    private readonly object _syncRoot = new();
    private FileLogService _inner;

    public SwitchableLogService(string logFilePath)
    {
        _inner = CreateInner(logFilePath);
    }

    public event EventHandler<LogEntry>? EntryWritten;

    public void SwitchTo(string logFilePath)
    {
        lock (_syncRoot)
        {
            _inner = CreateInner(logFilePath);
        }
    }

    public void Info(string message) => Write(log => log.Info(message));

    public void Error(string message, Exception? exception = null) => Write(log => log.Error(message, exception));

    private void Write(Action<FileLogService> write)
    {
        FileLogService log;
        lock (_syncRoot)
        {
            log = _inner;
        }

        write(log);
    }

    private FileLogService CreateInner(string logFilePath)
    {
        var log = new FileLogService(logFilePath);
        log.EntryWritten += (_, entry) => EntryWritten?.Invoke(this, entry);
        return log;
    }
}
