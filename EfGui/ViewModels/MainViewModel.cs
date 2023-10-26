using System.Windows.Input;

namespace EfGui.ViewModels;

public class MainViewModel : ViewModelBase
{
    public ICommand ListMigrations { get; set; }
    public ICommand AddMigration { get; set; }
    public ICommand RemoveLastMigration { get; set; }
    public ICommand GenerateScript { get; set; }
}
