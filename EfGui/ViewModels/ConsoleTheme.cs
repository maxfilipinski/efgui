using Avalonia.Media;

namespace EfGui.ViewModels;

public sealed record ConsoleTheme(string Name, string Hex)
{
    public IBrush Swatch => new SolidColorBrush(Color.Parse(Hex));
}
