namespace EfGui.Core.Tests;

public sealed class ErrorLogTests : IDisposable
{
    private readonly string _dir = Path.Combine(Path.GetTempPath(), "EfGuiTests", Guid.NewGuid().ToString("N"));

    private string FilePath => Path.Combine(_dir, "logs", "errors.log");

    public void Dispose()
    {
        if (Directory.Exists(_dir))
        {
            Directory.Delete(_dir, recursive: true);
        }
    }

    [Fact]
    public void Appends_exception_details_and_creates_the_folder()
    {
        var log = new ErrorLog(FilePath);

        Assert.True(log.TryAppend(new InvalidOperationException("first")));
        Assert.True(log.TryAppend(new InvalidOperationException("second")));

        var content = File.ReadAllText(FilePath);
        Assert.Contains("InvalidOperationException: first", content);
        Assert.Contains("InvalidOperationException: second", content);
    }

    [Fact]
    public void Oversized_log_is_rotated()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
        File.WriteAllText(FilePath, new string('x', 1024 * 1024 + 1));

        new ErrorLog(FilePath).TryAppend(new InvalidOperationException("fresh"));

        Assert.True(File.Exists(FilePath + ".old"));
        Assert.DoesNotContain("xxx", File.ReadAllText(FilePath));
    }

    [Fact]
    public void Write_failure_returns_false_instead_of_throwing()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
        using var locked = new FileStream(FilePath, FileMode.Create, FileAccess.Write, FileShare.None);

        Assert.False(new ErrorLog(FilePath).TryAppend(new InvalidOperationException("lost")));
    }
}
