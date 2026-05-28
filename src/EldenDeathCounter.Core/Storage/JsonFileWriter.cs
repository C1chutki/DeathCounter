namespace EldenDeathCounter.Core.Storage;

public static class JsonFileWriter
{
    public static async Task WriteAtomicAsync(string filePath, Func<Stream, Task> writeAsync)
    {
        var directory = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var tempPath = Path.Combine(
            string.IsNullOrWhiteSpace(directory) ? "." : directory,
            $"{Path.GetFileName(filePath)}.{Guid.NewGuid():N}.tmp");

        try
        {
            await using (var stream = new FileStream(
                tempPath,
                FileMode.CreateNew,
                FileAccess.Write,
                FileShare.None,
                bufferSize: 4096,
                FileOptions.WriteThrough | FileOptions.Asynchronous))
            {
                await writeAsync(stream);
                await stream.FlushAsync();
            }

            if (File.Exists(filePath))
            {
                File.Replace(tempPath, filePath, destinationBackupFileName: null);
            }
            else
            {
                File.Move(tempPath, filePath, overwrite: false);
            }
        }
        finally
        {
            // After a successful replace/move the temp file is already gone; this only fires when a
            // write failed before the swap. Swallow delete failures so they cannot mask the original
            // exception (e.g. when the temp file is briefly locked by antivirus).
            try
            {
                if (File.Exists(tempPath))
                {
                    File.Delete(tempPath);
                }
            }
            catch (IOException)
            {
            }
            catch (UnauthorizedAccessException)
            {
            }
        }
    }
}
