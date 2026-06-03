using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using EfGui.ViewModels;

namespace EfGui.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        DataContextChanged += (_, _) =>
        {
            if (DataContext is MainWindowViewModel vm)
                SidebarColumn.Width = new GridLength(vm.SidebarWidth);
        };

        SidebarSplitter.DragCompleted += SidebarSplitter_DragCompleted;
    }

    private ColumnDefinition SidebarColumn => RootGrid.ColumnDefinitions[0];

    private void SidebarSplitter_DragCompleted(object? sender, VectorEventArgs e)
    {
        if (DataContext is MainWindowViewModel vm)
            vm.SidebarWidth = SidebarColumn.ActualWidth;
    }

    private void SidebarSplitter_DoubleTapped(object? sender, RoutedEventArgs e)
    {
        SidebarColumn.Width = new GridLength(MainWindowViewModel.DefaultSidebarWidth);
        if (DataContext is MainWindowViewModel vm)
            vm.SidebarWidth = MainWindowViewModel.DefaultSidebarWidth;
    }
}
