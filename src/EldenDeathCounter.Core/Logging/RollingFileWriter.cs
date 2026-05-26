using System.Text;

namespace EldenDeathCounter.Core.Logging;

public sealed class RollingFileWriter
{
    private readonly object _syncRoot = new();
    private readonly string _filePath;
    private readonly long _maxBytes;
    private readonly int _retainedFiles;

    public RollingFileWriter(string filePath, long maxBytes, int retainedFiles)
    {
        _filePath = filePath;
        _maxBytes = Math.Max(1, maxBytes);
        _retainedFiles = Math.Max(0, retainedFiles);
        var directory = Path.GetDirectoryName(_filePath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }
    }

    public void WriteLine(string line)
    {
        var content = line + Environment.NewLine;
        var byteCount = Encoding.UTF8.GetByteCount(content);
        lock (_syncRoot)
        {
            if (File.Exists(_filePath) && new FileInfo(_filePath).Length + byteCount > _maxBytes)
            {
                Rotate();
            }

            File.AppendAllText(_filePath, content, Encoding.UTF8);
        }
    }

    private void Rotate()
    {
        if (_retainedFiles == 0)
        {
            File.Delete(_filePath);
            return;
        }

        var oldest = GetArchivePath(_retainedFiles);
        if (File.Exists(oldest))
        {
            File.Delete(oldest);
        }

        for (var index = _retainedFiles - 1; index >= 1; index--)
        {
            var source = GetArchivePath(index);
            if (File.Exists(source))
            {
                File.Move(source, GetArchivePath(index + 1), overwrite: true);
            }
        }

        if (File.Exists(_filePath))
        {
            File.Move(_filePath, GetArchivePath(1), overwrite: true);
        }
    }

    private string GetArchivePath(int index) => $"{_filePath}.{index}";
}
