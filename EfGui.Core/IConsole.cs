namespace EfGui.Core;

public interface IConsole
{
    void WriteLine(ConsoleMessageKind kind, string text);
    void Clear();
}
