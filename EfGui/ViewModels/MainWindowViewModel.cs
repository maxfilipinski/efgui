using Avalonia.Media;
using EfGui.Core.Actions;
using EfGui.Core.Engine;
using EfGui.Core.Profiles;
using EfGui.Core.Services;
using EfGui.Core.Settings;
using ReactiveUI;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Reactive;
using System.Reactive.Linq;
using System.Windows.Input;

namespace EfGui.ViewModels;

public sealed class MainWindowViewModel : ViewModelBase
{
    public static readonly IReadOnlyList<ConsoleTheme> ConsoleThemePresets = new[]
    {
        new ConsoleTheme("Black", "#0C0C0C"),
        new ConsoleTheme("Dark gray", "#1E1E1E"),
        new ConsoleTheme("Navy", "#0C2B4E")
    };

    private readonly SettingsStore _store;
    private readonly IConsole _console;

    // Recreated per operation; never disposed so a late Stop click is a safe no-op
    // rather than an ObjectDisposedException race.
    private CancellationTokenSource? _operation;

    private Profile? _selectedProfile;
    private ConsoleTheme? _selectedConsoleTheme;
    private string _consoleBackgroundHex = ConsoleThemePresets[0].Hex;
    private string _migrationName = "";
    private bool _isBusy;
    private double _consoleFontSize = 13;

    public MainWindowViewModel(SettingsStore store, MigrationActions actions, IConsole console)
    {
        _store = store;
        _console = console;

        Profiles = new ObservableCollection<Profile>(store.Profiles);
        _selectedProfile = store.LastSelectedProfile;
        _consoleBackgroundHex = store.ConsoleBackground;
        _consoleFontSize = store.ConsoleFontSize;
        // Null when the stored hex was hand-edited to a non-preset value; the brush still honors it.
        _selectedConsoleTheme = ConsoleThemePresets.FirstOrDefault(t => t.Hex == _consoleBackgroundHex);

        store.UnencryptedSecretSaved += message => console.WriteLine(ConsoleMessageKind.Error, message);
        if (store.LoadError != null)
            console.WriteLine(ConsoleMessageKind.Error, store.LoadError + " Changes will not be saved this session.");

        var notBusy = this.WhenAnyValue(x => x.IsBusy).Select(b => !b);
        var canRun = this.WhenAnyValue(x => x.SelectedProfile, x => x.IsBusy,
            (profile, busy) => profile != null && !busy);
        var canCreate = this.WhenAnyValue(x => x.SelectedProfile, x => x.MigrationName, x => x.IsBusy,
            (profile, name, busy) => profile != null && !busy && EfGui.Core.Engine.MigrationName.IsValid(name));

        AddProfile = ReactiveCommand.CreateFromTask(async () =>
        {
            var result = await ShowProfileEditor.Handle(null);
            if (result?.Saved != null)
                ApplyProfileSaved(result.Saved);
        }, notBusy);

        EditProfile = ReactiveCommand.CreateFromTask(async () =>
        {
            if (SelectedProfile is not { } profile)
                return;
            var result = await ShowProfileEditor.Handle(profile);
            if (result?.Saved != null)
                ApplyProfileSaved(result.Saved);
            else if (result?.Deleted == true)
                ApplyProfileDeleted(profile.Id);
        }, canRun);

        CreateMigration = EfCommand(ct => WithProfile(p => actions.CreateMigrationAsync(p, MigrationName, ct)), canCreate);
        Verify = EfCommand(ct => WithProfile(p => actions.VerifyAsync(p, ct)), canRun);
        ListMigrations = EfCommand(ct => WithProfile(p => actions.ListMigrationsAsync(p, ct)), canRun);
        GenerateFullScript = EfCommand(ct => WithScript(p => actions.GenerateFullScriptAsync(p, ct)), canRun);
        GenerateUnappliedScript = EfCommand(ct => WithScript(p => actions.GenerateUnappliedScriptAsync(p, ct)), canRun);
        GenerateOptimizedModel = EfCommand(ct => WithProfile(p => actions.GenerateOptimizedModelAsync(p, ct)), canRun);
        GenerateApplyScript = EfCommand(ct => WithScript(p => actions.GenerateApplyScriptAsync(p, ct)), canRun);
        GenerateRollbackScript = EfCommand(ct => WithScript(p => actions.GenerateRollbackScriptAsync(p, ct)), canRun);

        RemoveLastFromCode = EfCommand(
            ct => WithProfile(p => actions.RemoveLastFromCodeAsync(p, ct)), canRun,
            confirm: ("Remove migration",
                "This permanently deletes the most recent migration's files from your project. "
                + "EF refuses if the migration has been applied to the database. Continue?"));

        RecreateAndGenerateScript = EfCommand(
            ct => WithScript(p => actions.RecreateAndGenerateScriptAsync(p, ct)), canRun,
            confirm: ("Recreate migration",
                "This removes the most recent migration and re-adds it, overwriting its files. Continue?"));

        CancelOperation = ReactiveCommand.Create(
            () => _operation?.Cancel(), this.WhenAnyValue(x => x.IsBusy));

        ClearConsole = ReactiveCommand.Create(console.Clear);
    }

    public ObservableCollection<Profile> Profiles { get; }

    public Profile? SelectedProfile
    {
        get => _selectedProfile;
        set
        {
            this.RaiseAndSetIfChanged(ref _selectedProfile, value);
            this.RaisePropertyChanged(nameof(WindowTitle));
            this.RaisePropertyChanged(nameof(ConsoleHintText));
            if (value != null)
                Persist(s => s.SetLastSelected(value.Id));
        }
    }

    public string WindowTitle =>
        _selectedProfile is null ? "EfGui" : $"{_selectedProfile.Name} — EfGui";

    public string ConsoleHintText =>
        _selectedProfile is null
            ? "Add a profile to get started"
            : "Run an action to see its output here\nCtrl+scroll to zoom";

    [SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "Bound from XAML, which needs an instance member.")]
    public IReadOnlyList<ConsoleTheme> ConsoleThemes => ConsoleThemePresets;

    public ConsoleTheme? SelectedConsoleTheme
    {
        get => _selectedConsoleTheme;
        set
        {
            this.RaiseAndSetIfChanged(ref _selectedConsoleTheme, value);
            if (value != null)
            {
                _consoleBackgroundHex = value.Hex;
                Persist(s => s.SetConsoleBackground(value.Hex));
                this.RaisePropertyChanged(nameof(ConsoleBackground));
            }
        }
    }

    public IBrush ConsoleBackground => new SolidColorBrush(
        Color.TryParse(_consoleBackgroundHex, out var color) ? color : Color.Parse(ConsoleThemePresets[0].Hex));

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

    public const double DefaultSidebarWidth = 240;

    public double SidebarWidth
    {
        get => _store.SidebarWidth;
        set => Persist(s => s.SetSidebarWidth(value));
    }

    public (double X, double Y, double Width, double Height)? GetWindowBounds() =>
        _store.WindowBounds;

    public void SaveWindowBounds(double x, double y, double width, double height) =>
        _store.SetWindowBounds(x, y, width, height);

    public double ConsoleFontSize
    {
        get => _consoleFontSize;
        set
        {
            var clamped = Math.Clamp(value, 10, 24);
            this.RaiseAndSetIfChanged(ref _consoleFontSize, clamped);
            this.RaisePropertyChanged(nameof(ConsoleLineHeight));
            Persist(s => s.SetConsoleFontSize(clamped));
        }
    }

    public double ConsoleLineHeight => _consoleFontSize * 1.4;

    // Handled by the view: the profile editor (input null = add), the confirm dialog, and
    // opening a generated file in its associated application.
    public Interaction<Profile?, ProfileEditorResult?> ShowProfileEditor { get; } = new();
    public Interaction<(string Title, string Message), bool> Confirm { get; } = new();
    public Interaction<string, Unit> OpenFile { get; } = new();

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

    private Task WithProfile(Func<Profile, Task> body) =>
        SelectedProfile is { } profile ? body(profile) : Task.CompletedTask;

    private Task WithScript(Func<Profile, Task<string?>> generate) =>
        WithProfile(async profile =>
        {
            if (await generate(profile) is { } path)
                await OpenFile.Handle(path);
        });

    // Serializes EF operations behind IsBusy, optionally confirms first, and surfaces failures.
    private ReactiveCommand<Unit, Unit> EfCommand(
        Func<CancellationToken, Task> run,
        IObservable<bool> canExecute,
        (string Title, string Message)? confirm = null)
    {
        return ReactiveCommand.CreateFromTask(async () =>
        {
            if (confirm is { } c && !await Confirm.Handle(c))
                return;

            var cts = new CancellationTokenSource();
            _operation = cts;
            IsBusy = true;
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
                _console.WriteLine(ConsoleMessageKind.Error, ex.Message);
            }
            finally
            {
                IsBusy = false;
                _operation = null;
            }
        }, canExecute);
    }

    // The in-memory state is already updated when a save fails, so the UI stays consistent
    // and the next successful save persists it.
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

    public void ApplyProfileSaved(Profile profile)
    {
        var existing = Profiles.FirstOrDefault(p => p.Id == profile.Id);
        if (existing is null)
        {
            Persist(s => s.Add(profile));
            Profiles.Add(profile);
        }
        else
        {
            Persist(s => s.Update(profile));
            Profiles[Profiles.IndexOf(existing)] = profile;
        }

        SelectedProfile = profile;
    }

    public void ApplyProfileDeleted(Guid profileId)
    {
        Persist(s => s.Remove(profileId));
        try
        {
            HelperProjectGenerator.Delete(profileId);
        }
        catch (Exception ex) when (SettingsStore.IsFileAccessError(ex))
        {
            _console.WriteLine(ConsoleMessageKind.Error, $"Could not delete the profile's helper project: {ex.Message}");
        }

        var existing = Profiles.FirstOrDefault(p => p.Id == profileId);
        if (existing != null)
            Profiles.Remove(existing);

        SelectedProfile = Profiles.FirstOrDefault();
    }
}
