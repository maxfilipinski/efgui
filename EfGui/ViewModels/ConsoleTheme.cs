using Avalonia.Media;
using Avalonia.Media.Immutable;

namespace EfGui.ViewModels;

public sealed record ConsoleTheme(string Name, string Hex)
{
    public IBrush Swatch { get; } = new ImmutableSolidColorBrush(Color.Parse(Hex));
}
