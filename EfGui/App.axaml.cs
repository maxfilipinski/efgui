using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using EfGui.Core;
using EfGui.Core.Actions;
using EfGui.Core.Engine;
using EfGui.Core.Services;
using EfGui.Core.Settings;
using EfGui.ViewModels;
using EfGui.Views;

namespace EfGui;

public sealed partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = CreateMainWindow();
        }

        base.OnFrameworkInitializationCompleted();
    }

    private static MainWindow CreateMainWindow()
    {
        var mainWindow = new MainWindow();
        var store = new SettingsStore();
        var console = mainWindow.CreateConsoleRenderer();
        var processRunner = new ProcessRunner(console);
        var efRunner = new DotnetEfRunner(processRunner, console, new DotnetEfInstaller(processRunner, console));
        var actions = new MigrationActions(efRunner, console, AppPaths.ScriptsDir);
        actions.DeleteScriptsOlderThan(TimeSpan.FromDays(30));

        mainWindow.DataContext = new MainWindowViewModel(store, actions, console);
        return mainWindow;
    }
}
