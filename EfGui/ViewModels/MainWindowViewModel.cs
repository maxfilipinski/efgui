using Avalonia.Media;
using EfGui.Profiles;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;

namespace EfGui.ViewModels;

public record ConsoleTheme(string Name, string Hex);

public class MainWindowViewModel : ViewModelBase
{
    public static readonly IReadOnlyList<ConsoleTheme> ConsoleThemePresets = new[]
    {
        new ConsoleTheme("Black", "#0C0C0C"),
        new ConsoleTheme("Dark gray", "#1E1E1E"),
        new ConsoleTheme("Navy", "#0C2B4E")
    };

    private readonly ProfileStore? _store;
    private Profile? _selectedProfile;
    private ConsoleTheme? _selectedConsoleTheme;
    private string _consoleBackgroundHex = ConsoleThemePresets[0].Hex;
    private string _migrationName = "";
    private bool _isBusy;

    public MainWindowViewModel()
    {
    }

    public MainWindowViewModel(ProfileStore store)
    {
        _store = store;
        Profiles = new ObservableCollection<Profile>(store.Profiles);
        _selectedProfile = store.LastSelectedProfile;
        _consoleBackgroundHex = store.ConsoleBackground;
        // Null when the stored hex was hand-edited to a non-preset value; the brush still honors it.
        _selectedConsoleTheme = ConsoleThemePresets.FirstOrDefault(t => t.Hex == _consoleBackgroundHex);
    }

    public ObservableCollection<Profile> Profiles { get; } = new();

    public Profile? SelectedProfile
    {
        get => _selectedProfile;
        set
        {
            this.RaiseAndSetIfChanged(ref _selectedProfile, value);
            if (value != null)
                _store?.SetLastSelected(value.Id);
        }
    }

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
                _store?.SetConsoleBackground(value.Hex);
                this.RaisePropertyChanged(nameof(ConsoleBackground));
            }
        }
    }

    public IBrush ConsoleBackground => new SolidColorBrush(Color.Parse(_consoleBackgroundHex));

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

    public ICommand? AddProfile { get; set; }
    public ICommand? EditProfile { get; set; }
    public ICommand? CreateMigration { get; set; }
    public ICommand? ListMigrations { get; set; }
    public ICommand? GenerateFullScript { get; set; }
    public ICommand? GenerateUnappliedScript { get; set; }
    public ICommand? GenerateOptimizedModel { get; set; }
    public ICommand? RemoveLastFromCode { get; set; }
    public ICommand? RecreateAndGenerateScript { get; set; }
    public ICommand? GenerateApplyScript { get; set; }
    public ICommand? GenerateRollbackScript { get; set; }
    public ICommand? CancelOperation { get; set; }
    public ICommand? ClearConsole { get; set; }

    public void ApplyProfileSaved(Profile profile)
    {
        var existing = Profiles.FirstOrDefault(p => p.Id == profile.Id);
        if (existing is null)
        {
            _store?.Add(profile);
            Profiles.Add(profile);
        }
        else
        {
            _store?.Update(profile);
            Profiles[Profiles.IndexOf(existing)] = profile;
        }

        SelectedProfile = profile;
    }

    public void ApplyProfileDeleted(Guid profileId)
    {
        _store?.Remove(profileId);

        var existing = Profiles.FirstOrDefault(p => p.Id == profileId);
        if (existing != null)
            Profiles.Remove(existing);

        SelectedProfile = Profiles.FirstOrDefault();
    }
}
