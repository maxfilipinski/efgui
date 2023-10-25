using System.Windows.Input;

namespace EfGui.ViewModels;

public class MainViewModel : ViewModelBase
{
    public ICommand ListMigrations { get; set; }
}
