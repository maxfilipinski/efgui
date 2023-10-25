using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using EfGui.Actions;
using EfGui.ViewModels;
using EfGui.Views;
using ReactiveUI;

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
        var mainViewModel = new MainViewModel();
        var logger = new Logger(mainWindow.MainView.ScrollViewer, mainWindow.MainView.SelectableTextBlock);

        mainViewModel.ListMigrations = ReactiveCommand.CreateFromTask(async () =>
        {
            var action = new ListMigrationsAction();
            await action.ExecuteAsync(logger);
        });

        mainWindow.DataContext = mainViewModel;

        return mainWindow;
    }
}
