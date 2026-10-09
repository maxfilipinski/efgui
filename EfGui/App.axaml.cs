using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using EfGui.Core;
using EfGui.Core.DotnetEf;
using EfGui.Core.Migrations;
using EfGui.Core.Processes;
using EfGui.Core.Settings;
using EfGui.Services;
using EfGui.ViewModels;
using EfGui.Views;

namespace EfGui;

public sealed partial class App : Application
{
    private readonly ErrorLog _errorLog = new(AppPaths.ErrorLogFile);
    private ConsoleRenderer? _console;

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        Dispatcher.UIThread.UnhandledException += (_, args) =>
        {
            ReportUnexpectedError(args.Exception);
            args.Handled = true;
        };
        TaskScheduler.UnobservedTaskException += (_, args) =>
        {
            ReportUnexpectedError(args.Exception);
            args.SetObserved();
        };
        AppDomain.CurrentDomain.UnhandledException += (_, args) =>
        {
            if (args.ExceptionObject is Exception exception)
            {
                _errorLog.TryAppend(exception);
            }
        };

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = CreateMainWindow();
        }

        base.OnFrameworkInitializationCompleted();
    }

    private MainWindow CreateMainWindow()
    {
        var mainWindow = new MainWindow();
        var store = new SettingsStore();
        var console = mainWindow.CreateConsoleRenderer();
        var processRunner = new ProcessRunner(console);
        var efRunner = new DotnetEfRunner(processRunner, console, new DotnetEfInstaller(processRunner, console));
        var actions = new MigrationActions(efRunner, console, AppPaths.ScriptsDir);
        actions.DeleteScriptsOlderThan(TimeSpan.FromDays(30));

        mainWindow.DataContext = new MainWindowViewModel(store, actions, console);
        _console = console;
        return mainWindow;
    }

    private void ReportUnexpectedError(Exception exception)
    {
        var details = _errorLog.TryAppend(exception)
            ? $"Details were written to {_errorLog.FilePath}."
            : "Details could not be written to the error log.";
        _console?.WriteLine(ConsoleMessageKind.Error, $"Unexpected error: {exception.Message} {details}");
    }
}
