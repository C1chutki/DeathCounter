namespace EldenDeathCounter.Core.Logging;

public sealed class FileLogService : ILogService, ILogEntrySource
{
    private readonly object _syncRoot = new();
    private readonly string _logFilePath;
    private readonly RollingFileWriter _writer;

    public FileLogService(string logFilePath)
    {
        _logFilePath = logFilePath;
        var directory = Path.GetDirectoryName(_logFilePath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        _writer = new RollingFileWriter(_logFilePath, maxBytes: 2 * 1024 * 1024, retainedFiles: 5);
    }

    public event EventHandler<LogEntry>? EntryWritten;

    public void Info(string message) => Write("INFO", message, null);

    public void Error(string message, Exception? exception = null) => Write("ERROR", message, exception);

    private void Write(string level, string message, Exception? exception)
    {
        var timestamp = DateTimeOffset.Now;
        var entry = new LogEntry(timestamp, level, message);
        var line = $"{timestamp:O} [{level}] {message}";
        if (exception is not null)
        {
            line += $"{Environment.NewLine}{exception}";
        }

        lock (_syncRoot)
        {
            _writer.WriteLine(line);
        }

        EntryWritten?.Invoke(this, entry);
    }
}
