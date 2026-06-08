using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using EfGui.ViewModels;
using System;
using System.Linq;

namespace EfGui.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        DataContextChanged += (_, _) =>
        {
            if (DataContext is MainWindowViewModel vm)
            {
                SidebarColumn.Width = new GridLength(vm.SidebarWidth);
                RestoreWindowBounds(vm);
            }
        };

        Closing += (_, _) =>
        {
            try
            {
                if (WindowState == WindowState.Normal && DataContext is MainWindowViewModel vm)
                    vm.SaveWindowBounds(Position.X, Position.Y, ClientSize.Width, ClientSize.Height);
            }
            catch
            {
                // Saving bounds must never block or crash shutdown.
            }
        };

        SidebarSplitter.DragCompleted += SidebarSplitter_DragCompleted;

        // Tunnel so Ctrl+scroll zooms the console instead of scrolling it.
        ScrollViewer.AddHandler(PointerWheelChangedEvent, Console_PointerWheelChanged,
            RoutingStrategies.Tunnel);
    }

    // Keeps named-control access inside the window instead of leaking it to App.
    internal ConsoleRenderer CreateConsoleRenderer() =>
        new(ScrollViewer, SelectableTextBlock, ConsoleHint);

    private void RestoreWindowBounds(MainWindowViewModel vm)
    {
        if (vm.GetWindowBounds() is not { } bounds)
            return;

        // Ignore stale bounds pointing at a disconnected monitor.
        var probe = new PixelPoint((int)bounds.X + 24, (int)bounds.Y + 24);
        if (!Screens.All.Any(s => s.Bounds.Contains(probe)))
            return;

        WindowStartupLocation = WindowStartupLocation.Manual;
        Position = new PixelPoint((int)bounds.X, (int)bounds.Y);
        Width = bounds.Width;
        Height = bounds.Height;
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

    private void ConsoleThemePreset_Click(object? sender, RoutedEventArgs e)
    {
        if (sender is Button { DataContext: ConsoleTheme theme }
            && DataContext is MainWindowViewModel vm)
        {
            vm.SelectedConsoleTheme = theme;
            ConsoleThemeButton.Flyout?.Hide();
        }
    }
}
