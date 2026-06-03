using EfGui.Profiles;
using ReactiveUI;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;

namespace EfGui.ViewModels;

public class MainWindowViewModel : ViewModelBase
{
    private readonly ProfileStore? _store;
    private Profile? _selectedProfile;

    public MainWindowViewModel()
    {
    }

    public MainWindowViewModel(ProfileStore store)
    {
        _store = store;
        Profiles = new ObservableCollection<Profile>(store.Profiles);
        _selectedProfile = store.LastSelectedProfile;
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

    public ICommand? AddProfile { get; set; }
    public ICommand? EditProfile { get; set; }
    public ICommand? ListMigrations { get; set; }
    public ICommand? AddMigration { get; set; }
    public ICommand? RemoveLastMigration { get; set; }
    public ICommand? GenerateScript { get; set; }

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
