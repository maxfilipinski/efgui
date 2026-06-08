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
using System;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;

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
        var console = mainWindow.CreateConsoleRenderer();
        var runner = new ProcessRunner(console);
        var engine = new EfCoreEngine(runner, console, new DotnetEfTool(runner, console));
        var notBusy = mainWindowViewModel.WhenAnyValue(vm => vm.IsBusy).Select(b => !b);
        var canRun = mainWindowViewModel
            .WhenAnyValue(vm => vm.SelectedProfile, vm => vm.IsBusy,
                (profile, busy) => profile != null && !busy);

        CancellationTokenSource? currentOperation = null;

        // Serializes EF operations behind IsBusy and surfaces failures in the console.
        ICommand EfCommand(Func<CancellationToken, Task> run, IObservable<bool>? canExecute = null) =>
            ReactiveCommand.CreateFromTask(
                async () =>
                {
                    using var cts = new CancellationTokenSource();
                    currentOperation = cts;
                    mainWindowViewModel.IsBusy = true;
                    try
                    {
                        await run(cts.Token);
                    }
                    catch (OperationCanceledException)
                    {
                        // Already reported by ProcessRunner.
                    }
                    catch (Exception ex)
                    {
                        console.WriteLine(ConsoleMessageKind.Error, ex.Message);
                    }
                    finally
                    {
                        mainWindowViewModel.IsBusy = false;
                        currentOperation = null;
                    }
                },
                canExecute ?? canRun);

        mainWindowViewModel.AddProfile = ReactiveCommand.CreateFromTask(
            async () =>
            {
                var editor = new ProfileEditorWindow(new ProfileEditorViewModel());
                var result = await editor.ShowDialog<ProfileEditorResult?>(mainWindow);
                if (result?.Saved != null)
                    mainWindowViewModel.ApplyProfileSaved(result.Saved);
            },
            notBusy);

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
            canRun);

        var actions = new MigrationActions(engine, console);
        Profile Selected() => mainWindowViewModel.SelectedProfile!;

        var canCreateMigration = mainWindowViewModel
            .WhenAnyValue(vm => vm.SelectedProfile, vm => vm.MigrationName, vm => vm.IsBusy,
                (profile, name, busy) => profile != null && !string.IsNullOrWhiteSpace(name) && !busy);

        mainWindowViewModel.CreateMigration = EfCommand(
            ct => actions.CreateMigrationAsync(Selected(), mainWindowViewModel.MigrationName, ct),
            canCreateMigration);

        mainWindowViewModel.ListMigrations = EfCommand(
            ct => actions.ListMigrationsAsync(Selected(), ct));

        mainWindowViewModel.GenerateFullScript = EfCommand(
            ct => actions.GenerateFullScriptAsync(Selected(), ct));

        mainWindowViewModel.GenerateUnappliedScript = EfCommand(
            ct => actions.GenerateUnappliedScriptAsync(Selected(), ct));

        mainWindowViewModel.GenerateOptimizedModel = EfCommand(
            ct => actions.GenerateOptimizedModelAsync(Selected(), ct));

        mainWindowViewModel.RemoveLastFromCode = EfCommand(
            ct => actions.RemoveLastFromCodeAsync(Selected(), ct));

        mainWindowViewModel.RecreateAndGenerateScript = EfCommand(
            ct => actions.RecreateAndGenerateScriptAsync(Selected(), ct));

        mainWindowViewModel.GenerateApplyScript = EfCommand(
            ct => actions.GenerateApplyScriptAsync(Selected(), ct));

        mainWindowViewModel.GenerateRollbackScript = EfCommand(
            ct => actions.GenerateRollbackScriptAsync(Selected(), ct));

        mainWindowViewModel.CancelOperation = ReactiveCommand.Create(
            () => currentOperation?.Cancel(),
            mainWindowViewModel.WhenAnyValue(vm => vm.IsBusy));

        mainWindowViewModel.ClearConsole = ReactiveCommand.Create(console.Clear);

        mainWindow.DataContext = mainWindowViewModel;

        return mainWindow;
    }
}
