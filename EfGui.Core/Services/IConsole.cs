namespace EfGui.Core.Services;

public interface IConsole
{
    void WriteLine(ConsoleMessageKind kind, string text);
    void Clear();
}
