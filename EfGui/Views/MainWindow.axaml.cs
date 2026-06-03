using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using EfGui.ViewModels;
using System;

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

        // Tunnel so Ctrl+scroll zooms the console instead of scrolling it.
        ScrollViewer.AddHandler(PointerWheelChangedEvent, Console_PointerWheelChanged,
            RoutingStrategies.Tunnel);
    }

    private void Console_PointerWheelChanged(object? sender, PointerWheelEventArgs e)
    {
        if (!e.KeyModifiers.HasFlag(KeyModifiers.Control)
            || DataContext is not MainWindowViewModel vm)
            return;

        vm.ConsoleFontSize += Math.Sign(e.Delta.Y);
        e.Handled = true;
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
