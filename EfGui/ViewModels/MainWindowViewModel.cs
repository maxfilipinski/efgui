using Avalonia.Media;
using Avalonia.Media.Immutable;
using EfGui.Core;
using EfGui.Core.DotnetEf;
using EfGui.Core.Migrations;
using EfGui.Core.Profiles;
using EfGui.Core.Settings;
using ReactiveUI;
using ReactiveUI.Primitives;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace EfGui.ViewModels;

public sealed class MainWindowViewModel : ViewModelBase
{
    public const double DefaultSidebarWidth = 240;
    private const int MinConsoleFontSize = 10;
    private const int MaxConsoleFontSize = 24;
    private const double ConsoleLineHeightFactor = 1.4;

    public static readonly IReadOnlyList<ConsoleTheme> ConsoleThemePresets =
    [
        new("Black", "#0C0C0C"),
        new("Dark gray", "#1E1E1E"),
        new("Navy", "#0C2B4E")
    ];

    private readonly SettingsStore _store;
    private readonly IConsole _console;

    private CancellationTokenSource? _operation;

    private Profile? _selectedProfile;
    private ConsoleTheme? _selectedConsoleTheme;
    private IBrush _consoleBackground;
    private string _migrationName = "";
    private bool _isBusy;
    private double _consoleFontSize;

    public MainWindowViewModel(SettingsStore store, MigrationActions actions, IConsole console)
    {
        _store = store;
        _console = console;

        Profiles = [.. store.Profiles];
        _selectedProfile = store.LastSelectedProfile;
        _consoleBackground = CreateBrush(store.ConsoleBackground);
        _consoleFontSize = store.ConsoleFontSize;
        // Null for a hand-edited, non-preset hex; the brush still uses it.
        _selectedConsoleTheme = ConsoleThemePresets.FirstOrDefault(theme => theme.Hex == store.ConsoleBackground);

        store.UnencryptedSecretSaved += message => console.WriteLine(ConsoleMessageKind.Error, message);
        if (store.LoadError != null)
        {
            console.WriteLine(ConsoleMessageKind.Error, store.LoadError + " Changes will not be saved this session.");
        }

        var notBusy = this.WhenAnyValue(model => model.IsBusy).Select(isBusy => !isBusy);
        var canRun = this.WhenAnyValue(model => model.SelectedProfile, model => model.IsBusy,
            (profile, isBusy) => profile != null && !isBusy);
        var canCreate = this.WhenAnyValue(model => model.SelectedProfile, model => model.MigrationName,
            model => model.IsBusy,
            (profile, migrationName, isBusy) =>
                profile != null && !isBusy && EfGui.Core.Migrations.MigrationName.IsValid(migrationName));

        AddProfile = ReactiveCommand.CreateFromTask(async () =>
        {
            var result = await ShowProfileEditor.Handle(null);
            if (result?.Saved != null)
            {
                ApplyProfileSaved(result.Saved);
            }
        }, notBusy);

        EditProfile = ReactiveCommand.CreateFromTask(async () =>
        {
            if (SelectedProfile is not { } profile)
            {
                return;
            }

            var result = await ShowProfileEditor.Handle(profile);
            if (result?.Saved != null)
            {
                ApplyProfileSaved(result.Saved);
            }
            else if (result?.Deleted == true)
            {
                ApplyProfileDeleted(profile.Id);
            }
        }, canRun);

        CreateMigration = EfCommand(
            cancellationToken => WithProfile(profile =>
                actions.CreateMigrationAsync(profile, MigrationName, cancellationToken)),
            canCreate);
        Verify = EfCommand(
            cancellationToken => WithProfile(profile => actions.VerifyAsync(profile, cancellationToken)),
            canRun);
        ListMigrations = EfCommand(
            cancellationToken => WithProfile(profile => actions.ListMigrationsAsync(profile, cancellationToken)),
            canRun);
        GenerateFullScript = EfCommand(
            cancellationToken => WithScript(profile => actions.GenerateFullScriptAsync(profile, cancellationToken)),
            canRun);
        GenerateUnappliedScript = EfCommand(
            cancellationToken => WithScript(profile =>
                actions.GenerateUnappliedScriptAsync(profile, cancellationToken)),
            canRun);
        GenerateOptimizedModel = EfCommand(
            cancellationToken => WithProfile(profile =>
                actions.GenerateOptimizedModelAsync(profile, cancellationToken)),
            canRun);
        GenerateApplyScript = EfCommand(
            cancellationToken => WithScript(profile => actions.GenerateApplyScriptAsync(profile, cancellationToken)),
            canRun);
        GenerateRollbackScript = EfCommand(
            cancellationToken => WithScript(profile =>
                actions.GenerateRollbackScriptAsync(profile, cancellationToken)),
            canRun);

        RemoveLastFromCode = EfCommand(
            cancellationToken => WithProfile(profile =>
                actions.RemoveLastFromCodeAsync(profile, cancellationToken)),
            canRun,
            ("Remove migration",
                "This permanently deletes the most recent migration's files from your project. "
                + "EF refuses if the migration has been applied to the database. Continue?"));

        RecreateAndGenerateScript = EfCommand(
            cancellationToken => WithScript(profile =>
                actions.RecreateAndGenerateScriptAsync(profile, cancellationToken)),
            canRun,
            ("Recreate migration",
                "This removes the most recent migration and re-adds it, overwriting its files. Continue?"));

        CancelOperation = ReactiveCommand.Create(
            () => _operation?.Cancel(), this.WhenAnyValue(model => model.IsBusy));

        ClearConsole = ReactiveCommand.Create(console.Clear);
    }

    public ObservableCollection<Profile> Profiles { get; }

    public Profile? SelectedProfile
    {
        get => _selectedProfile;
        set
        {
            if (ReferenceEquals(_selectedProfile, value))
            {
                return;
            }

            this.RaiseAndSetIfChanged(ref _selectedProfile, value);
            this.RaisePropertyChanged(nameof(WindowTitle));
            this.RaisePropertyChanged(nameof(ConsoleHintText));
            if (value != null)
            {
                Persist(settings => settings.SetLastSelected(value.Id));
            }
        }
    }

    public string WindowTitle =>
        _selectedProfile is null ? "EfGui" : $"{_selectedProfile.Name} — EfGui";

    public string ConsoleHintText =>
        _selectedProfile is null
            ? "Add a profile to get started"
            : "Run an action to see its output here\nCtrl+scroll to zoom";

    public ConsoleTheme? SelectedConsoleTheme
    {
        get => _selectedConsoleTheme;
        set
        {
            this.RaiseAndSetIfChanged(ref _selectedConsoleTheme, value);
            if (value == null)
            {
                return;
            }

            _consoleBackground = CreateBrush(value.Hex);
            Persist(settings => settings.SetConsoleBackground(value.Hex));
            this.RaisePropertyChanged(nameof(ConsoleBackground));
        }
    }

    public IBrush ConsoleBackground => _consoleBackground;

    public string MigrationName
    {
        get => _migrationName;
        set => this.RaiseAndSetIfChanged(ref _migrationName, value);
    }

    public bool IsBusy
    {
        get => _isBusy;
        set => this.RaiseAndSetIfChanged(ref _isBusy, value);
    }

    public double SidebarWidth
    {
        get => _store.SidebarWidth;
        set => Persist(settings => settings.SetSidebarWidth(value));
    }

    public double ConsoleFontSize
    {
        get => _consoleFontSize;
        set
        {
            var clamped = Math.Clamp(value, MinConsoleFontSize, MaxConsoleFontSize);
            this.RaiseAndSetIfChanged(ref _consoleFontSize, clamped);
            this.RaisePropertyChanged(nameof(ConsoleLineHeight));
            Persist(settings => settings.SetConsoleFontSize(clamped));
        }
    }

    public double ConsoleLineHeight => _consoleFontSize * ConsoleLineHeightFactor;

    public Interaction<Profile?, ProfileEditorResult?> ShowProfileEditor { get; } = new();
    public Interaction<(string Title, string Message), bool> Confirm { get; } = new();
    public Interaction<string, RxVoid> OpenFile { get; } = new();

    public ICommand AddProfile { get; }
    public ICommand EditProfile { get; }
    public ICommand CreateMigration { get; }
    public ICommand Verify { get; }
    public ICommand ListMigrations { get; }
    public ICommand GenerateFullScript { get; }
    public ICommand GenerateUnappliedScript { get; }
    public ICommand GenerateOptimizedModel { get; }
    public ICommand RemoveLastFromCode { get; }
    public ICommand RecreateAndGenerateScript { get; }
    public ICommand GenerateApplyScript { get; }
    public ICommand GenerateRollbackScript { get; }
    public ICommand CancelOperation { get; }
    public ICommand ClearConsole { get; }

    public (double X, double Y, double Width, double Height)? GetWindowBounds()
    {
        return _store.WindowBounds;
    }

    public void SaveWindowBounds(double x, double y, double width, double height)
    {
        Persist(settings => settings.SetWindowBounds(x, y, width, height));
    }

    private void ApplyProfileSaved(Profile profile)
    {
        var existing = Profiles.FirstOrDefault(candidate => candidate.Id == profile.Id);
        if (existing is null)
        {
            Persist(settings => settings.Add(profile));
            Profiles.Add(profile);
        }
        else
        {
            Persist(settings => settings.Update(profile));
            Profiles[Profiles.IndexOf(existing)] = profile;
        }

        SelectedProfile = profile;
    }

    private void ApplyProfileDeleted(Guid profileId)
    {
        Persist(settings => settings.Remove(profileId));
        try
        {
            HelperProjectGenerator.Delete(profileId);
        }
        catch (Exception ex) when (SettingsStore.IsFileAccessError(ex))
        {
            _console.WriteLine(ConsoleMessageKind.Error,
                $"Could not delete the profile's helper project: {ex.Message}");
        }

        var existing = Profiles.FirstOrDefault(profile => profile.Id == profileId);
        if (existing != null)
        {
            Profiles.Remove(existing);
        }

        SelectedProfile = Profiles.FirstOrDefault();
    }

    private Task WithProfile(Func<Profile, Task> body)
    {
        return SelectedProfile is { } profile ? body(profile) : Task.CompletedTask;
    }

    private Task WithScript(Func<Profile, Task<string?>> generate)
    {
        return WithProfile(async profile =>
        {
            if (await generate(profile) is { } path)
            {
                await OpenFile.Handle(path);
            }
        });
    }

    private ReactiveCommand<RxVoid, RxVoid> EfCommand(
        Func<CancellationToken, Task> run,
        IObservable<bool> canExecute,
        (string Title, string Message)? confirm = null)
    {
        return ReactiveCommand.CreateFromTask(async () =>
        {
            if (confirm is { } confirmation && !await Confirm.Handle(confirmation))
            {
                return;
            }

            var operation = new CancellationTokenSource();
            _operation = operation;
            IsBusy = true;
            try
            {
                await run(operation.Token);
            }
            catch (OperationCanceledException)
            {
                // Already reported by ProcessRunner.
            }
            catch (Exception ex)
            {
                _console.WriteLine(ConsoleMessageKind.Error, ex.Message);
            }
            finally
            {
                IsBusy = false;
                _operation = null;
            }
        }, canExecute);
    }

    private static ImmutableSolidColorBrush CreateBrush(string hex)
    {
        return new ImmutableSolidColorBrush(
            Color.TryParse(hex, out var color) ? color : Color.Parse(ConsoleThemePresets[0].Hex));
    }

    // A failed save keeps the in-memory change; the next successful save persists it.
    private void Persist(Action<SettingsStore> save)
    {
        try
        {
            save(_store);
        }
        catch (Exception ex) when (SettingsStore.IsFileAccessError(ex))
        {
            _console.WriteLine(ConsoleMessageKind.Error, $"Could not save settings: {ex.Message}");
        }
    }
}
