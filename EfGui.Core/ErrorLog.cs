namespace EfGui.Core;

public sealed class ErrorLog
{
    private const long MaxBytes = 1024 * 1024;

    private readonly Lock _lock = new();

    public ErrorLog(string filePath)
    {
        FilePath = filePath;
    }

    public string FilePath { get; }

    // Must never throw: it runs while reporting another failure.
    public bool TryAppend(Exception exception)
    {
        try
        {
            lock (_lock)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
                if (File.Exists(FilePath) && new FileInfo(FilePath).Length > MaxBytes)
                {
                    File.Move(FilePath, FilePath + ".old", overwrite: true);
                }

                File.AppendAllText(FilePath, $"[{DateTimeOffset.Now:O}] {exception}{Environment.NewLine}{Environment.NewLine}");
            }

            return true;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return false;
        }
    }
}
