using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using EfGui.ViewModels;
using System.Linq;

namespace EfGui.Views;

public partial class ProfileEditorWindow : Window
{
    private bool _deleteArmed;

    public ProfileEditorWindow()
    {
        InitializeComponent();
    }

    public ProfileEditorWindow(ProfileEditorViewModel viewModel)
        : this()
    {
        DataContext = viewModel;
    }

    private ProfileEditorViewModel ViewModel => (ProfileEditorViewModel)DataContext!;

    private async void BrowseCsproj_Click(object? sender, RoutedEventArgs e)
    {
        var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Select project file",
            FileTypeFilter = new[]
            {
                new FilePickerFileType("C# project") { Patterns = new[] { "*.csproj" } }
            }
        });

        var path = files.FirstOrDefault()?.TryGetLocalPath();
        if (path != null)
            ViewModel.CsprojPath = path;
    }

    private void Save_Click(object? sender, RoutedEventArgs e)
    {
        var profile = ViewModel.TryBuildProfile();
        if (profile != null)
            Close(new ProfileEditorResult(profile, Deleted: false));
    }

    private void Cancel_Click(object? sender, RoutedEventArgs e)
    {
        Close(null);
    }

    private void Delete_Click(object? sender, RoutedEventArgs e)
    {
        // First click arms the button, second click confirms.
        if (!_deleteArmed)
        {
            _deleteArmed = true;
            DeleteButton.Content = "Really delete?";
            return;
        }

        Close(new ProfileEditorResult(null, Deleted: true));
    }
}
