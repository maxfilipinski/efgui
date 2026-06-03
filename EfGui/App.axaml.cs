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

        var actions = new MigrationActions(engine, console);
        Profile Selected() => mainWindowViewModel.SelectedProfile!;

        var canCreateMigration = mainWindowViewModel
            .WhenAnyValue(vm => vm.SelectedProfile, vm => vm.MigrationName,
                (profile, name) => profile != null && !string.IsNullOrWhiteSpace(name));

        mainWindowViewModel.CreateMigration = ReactiveCommand.CreateFromTask(
            () => actions.CreateMigrationAsync(Selected(), mainWindowViewModel.MigrationName),
            canCreateMigration);

        mainWindowViewModel.ListMigrations = ReactiveCommand.CreateFromTask(
            () => actions.ListMigrationsAsync(Selected()), hasProfile);

        mainWindowViewModel.GenerateFullScript = ReactiveCommand.CreateFromTask(
            () => actions.GenerateFullScriptAsync(Selected()), hasProfile);

        mainWindowViewModel.GenerateUnappliedScript = ReactiveCommand.CreateFromTask(
            () => actions.GenerateUnappliedScriptAsync(Selected()), hasProfile);

        mainWindowViewModel.GenerateOptimizedModel = ReactiveCommand.CreateFromTask(
            () => actions.GenerateOptimizedModelAsync(Selected()), hasProfile);

        mainWindowViewModel.RemoveLastFromCode = ReactiveCommand.CreateFromTask(
            () => actions.RemoveLastFromCodeAsync(Selected()), hasProfile);

        mainWindowViewModel.RecreateAndGenerateScript = ReactiveCommand.CreateFromTask(
            () => actions.RecreateAndGenerateScriptAsync(Selected()), hasProfile);

        mainWindowViewModel.GenerateApplyScript = ReactiveCommand.CreateFromTask(
            () => actions.GenerateApplyScriptAsync(Selected()), hasProfile);

        mainWindowViewModel.GenerateRollbackScript = ReactiveCommand.CreateFromTask(
            () => actions.GenerateRollbackScriptAsync(Selected()), hasProfile);

        mainWindowViewModel.ClearConsole = ReactiveCommand.Create(console.Clear);

        mainWindow.DataContext = mainWindowViewModel;

        return mainWindow;
    }
}
