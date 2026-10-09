using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using EfGui.ViewModels;

namespace EfGui.Views;

public sealed partial class ProfileEditorWindow : Window
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

    // async void event handler: an escaping exception would crash the app.
    private async void BrowseCsproj_Click(object? sender, RoutedEventArgs e)
    {
        try
        {
            var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = "Select project file",
                FileTypeFilter = [new FilePickerFileType("C# project") { Patterns = ["*.csproj"] }]
            });

            var path = files.Count > 0 ? files[0].TryGetLocalPath() : null;
            if (path != null)
            {
                ViewModel.CsprojPath = path;
            }
        }
        catch (Exception ex)
        {
            ViewModel.ShowError($"Could not open the file picker: {ex.Message}");
        }
    }

    private void Save_Click(object? sender, RoutedEventArgs e)
    {
        var profile = ViewModel.TryBuildProfile();
        if (profile != null)
        {
            Close(new ProfileEditorResult(profile, Deleted: false));
        }
    }

    private void Cancel_Click(object? sender, RoutedEventArgs e)
    {
        Close(null);
    }

    private void Delete_Click(object? sender, RoutedEventArgs e)
    {
        if (!_deleteArmed)
        {
            _deleteArmed = true;
            DeleteButton.Content = "Really delete?";
            return;
        }

        Close(new ProfileEditorResult(null, Deleted: true));
    }
}
