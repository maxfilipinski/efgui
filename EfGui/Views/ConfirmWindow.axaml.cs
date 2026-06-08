using Avalonia.Controls;
using Avalonia.Interactivity;
using System.Threading.Tasks;

namespace EfGui.Views;

public partial class ConfirmWindow : Window
{
    public ConfirmWindow()
    {
        InitializeComponent();
    }

    public static Task<bool> ShowAsync(Window owner, string title, string message)
    {
        var dialog = new ConfirmWindow { Title = title };
        dialog.MessageText.Text = message;
        return dialog.ShowDialog<bool>(owner);
    }

    private void Confirm_Click(object? sender, RoutedEventArgs e) => Close(true);

    private void Cancel_Click(object? sender, RoutedEventArgs e) => Close(false);
}
