using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using EfGui.Actions;
using EfGui.Engine;
using EfGui.Profiles;
using EfGui.Services;
using EfGui.ViewModels;
using EfGui.Views;
using ReactiveUI;
using System.Reactive.Linq;

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
        var profileStore = new ProfileStore();
        var mainWindowViewModel = new MainWindowViewModel(profileStore);
        var console = new ConsoleRenderer(mainWindow.ScrollViewer, mainWindow.SelectableTextBlock);
        var runner = new ProcessRunner(console);
        var engine = new EfCoreEngine(runner, console, new DotnetEfTool(runner, console));
        var hasProfile = mainWindowViewModel.WhenAnyValue(vm => vm.SelectedProfile).Select(p => p != null);

        mainWindowViewModel.AddProfile = ReactiveCommand.CreateFromTask(async () =>
        {
            var editor = new ProfileEditorWindow(new ProfileEditorViewModel());
            var result = await editor.ShowDialog<ProfileEditorResult?>(mainWindow);
            if (result?.Saved != null)
                mainWindowViewModel.ApplyProfileSaved(result.Saved);
        });

        mainWindowViewModel.EditProfile = ReactiveCommand.CreateFromTask(
            async () =>
            {
                var profile = mainWindowViewModel.SelectedProfile;
                if (profile is null)
                    return;

                var editor = new ProfileEditorWindow(new ProfileEditorViewModel(profile));
                var result = await editor.ShowDialog<ProfileEditorResult?>(mainWindow);
                if (result?.Saved != null)
                    mainWindowViewModel.ApplyProfileSaved(result.Saved);
                else if (result?.Deleted == true)
                    mainWindowViewModel.ApplyProfileDeleted(profile.Id);
            },
            hasProfile);

        mainWindowViewModel.ListMigrations = ReactiveCommand.CreateFromTask(
            async () =>
            {
                var action = new ListMigrationsAction();
                await action.ExecuteAsync(engine, mainWindowViewModel.SelectedProfile!);
            },
            hasProfile);

        mainWindowViewModel.AddMigration = ReactiveCommand.CreateFromTask(
            async () =>
            {
                var action = new AddMigrationAction();
                await action.ExecuteAsync(engine, mainWindowViewModel.SelectedProfile!, "example");
            },
            hasProfile);

        mainWindowViewModel.RemoveLastMigration = ReactiveCommand.CreateFromTask(
            async () =>
            {
                var action = new RemoveLastMigrationAction();
                await action.ExecuteAsync(engine, mainWindowViewModel.SelectedProfile!);
            },
            hasProfile);

        mainWindowViewModel.GenerateScript = ReactiveCommand.CreateFromTask(
            async () =>
            {
                var action = new GenerateScriptAction();
                await action.ExecuteAsync(engine, console, mainWindowViewModel.SelectedProfile!);
            },
            hasProfile);

        mainWindowViewModel.ClearConsole = ReactiveCommand.Create(console.Clear);

        mainWindow.DataContext = mainWindowViewModel;

        return mainWindow;
    }
}
