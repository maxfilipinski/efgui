using ReactiveUI;
using System.Windows.Input;

namespace EfGui.ViewModels;

public class MainWindowViewModel : ViewModelBase
{
    private ViewModelBase _contentViewModel;

    public MainWindowViewModel()
    {
    }

    public ProfileViewModel ProfileView { get; }

    public ViewModelBase ContentViewModel
    {
        get => _contentViewModel;
        private set => this.RaiseAndSetIfChanged(ref _contentViewModel, value);
    }

    public ICommand AddProfile { get; set; }
    public ICommand EditProfile { get; set; }
    public ICommand ListMigrations { get; set; }
    public ICommand AddMigration { get; set; }
    public ICommand RemoveLastMigration { get; set; }
    public ICommand GenerateScript { get; set; }
}