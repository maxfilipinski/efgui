using EfGui.Core.Services;

namespace EfGui.Core.Tests.Fakes;

public sealed class RecordingConsole : IConsole
{
    public List<(ConsoleMessageKind Kind, string Text)> Lines { get; } = new();

    public void WriteLine(ConsoleMessageKind kind, string text) => Lines.Add((kind, text));

    public void Clear() => Lines.Clear();

    public bool Contains(ConsoleMessageKind kind, string fragment) =>
        Lines.Any(l => l.Kind == kind && l.Text.Contains(fragment, StringComparison.Ordinal));
}
