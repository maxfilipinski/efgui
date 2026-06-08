using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using EfGui.Actions;
using EfGui.Engine;
using EfGui.Profiles;
using EfGui.Services;
using EfGui.ViewModels;
using EfGui.Views;

namespace EfGui;

public partial class App : Application
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

    private MainWindow CreateMainWindow()
    {
        var mainWindow = new MainWindow();
        var store = new ProfileStore();
        var console = mainWindow.CreateConsoleRenderer();
        var runner = new ProcessRunner(console);
        var engine = new EfCoreEngine(runner, console, new DotnetEfTool(runner, console));
        var actions = new MigrationActions(engine, console);

        var viewModel = new MainWindowViewModel(store, actions, console)
        {
            ShowProfileEditor = profile =>
            {
                var editor = new ProfileEditorWindow(new ProfileEditorViewModel(profile));
                return editor.ShowDialog<ProfileEditorResult?>(mainWindow);
            },
            ConfirmAsync = (title, message) => ConfirmWindow.ShowAsync(mainWindow, title, message)
        };

        mainWindow.DataContext = viewModel;
        return mainWindow;
    }
}
