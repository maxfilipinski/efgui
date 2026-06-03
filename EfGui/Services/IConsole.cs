namespace EfGui.Services;

public enum ConsoleMessageKind
{
    Command,
    StdOut,
    StdErr,
    Info,
    Success,
    Error
}

public interface IConsole
{
    void WriteLine(ConsoleMessageKind kind, string text);
    void Clear();
}
