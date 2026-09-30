using Avalonia;
using ReactiveUI.Avalonia;

namespace EfGui;

internal sealed class Program
{
    // Don't touch Avalonia or SynchronizationContext-dependent code before the lifetime starts.
    [STAThread]
    public static void Main(string[] args) => BuildAvaloniaApp()
        .StartWithClassicDesktopLifetime(args);

    // Also called by the XAML previewer, so it must stay public and static.
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace()
            .UseReactiveUI(_ => { });
}
