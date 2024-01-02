using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using EfGui.Actions;
using EfGui.ViewModels;
using EfGui.Views;
using ReactiveUI;
using System.Threading.Tasks;

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
        var mainWindowViewModel = new MainWindowViewModel();
        var logger = new Logger(mainWindow.ScrollViewer, mainWindow.SelectableTextBlock);

        mainWindowViewModel.AddProfile = ReactiveCommand.CreateFromTask(async () =>
        {
            var action = new AddProfileAction();
            await Task.Delay(10);
        });

        mainWindowViewModel.EditProfile = ReactiveCommand.CreateFromTask(async () =>
        {
            var action = new EditProfileAction();
            await Task.Delay(10);
        });

        mainWindowViewModel.ListMigrations = ReactiveCommand.CreateFromTask(async () =>
        {
            var action = new ListMigrationsAction();
            await action.ExecuteAsync(logger);
        });

        mainWindowViewModel.AddMigration = ReactiveCommand.CreateFromTask(async () =>
        {
            var action = new AddMigrationAction();
            await action.ExecuteAsync(logger);
        });

        mainWindowViewModel.RemoveLastMigration = ReactiveCommand.CreateFromTask(async () =>
        {
            var action = new RemoveLastMigrationAction();
            await action.ExecuteAsync(logger);
        });

        mainWindowViewModel.GenerateScript = ReactiveCommand.CreateFromTask(async () =>
        {
            var action = new GenerateScriptAction();
            await action.ExecuteAsync(logger);
        });

        mainWindow.DataContext = mainWindowViewModel;

        return mainWindow;
    }
}
