using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using EfGui.Services;
using EfGui.ViewModels;
using ReactiveUI;
using ReactiveUI.Avalonia;
using ReactiveUI.Primitives;

namespace EfGui.Views;

public sealed partial class MainWindow : ReactiveWindow<MainWindowViewModel>
{
    private const int VisibleCornerMargin = 24;

    public MainWindow()
    {
        InitializeComponent();

        this.WhenActivated(disposables =>
        {
            if (ViewModel is not { } viewModel)
            {
                return;
            }

            viewModel.ShowProfileEditor.RegisterHandler(async context =>
            {
                var editor = new ProfileEditorWindow(new ProfileEditorViewModel(context.Input));
                context.SetOutput(await editor.ShowDialog<ProfileEditorResult?>(this));
            }).DisposeWith(disposables);

            viewModel.Confirm.RegisterHandler(async context =>
                    context.SetOutput(await ConfirmWindow.ShowAsync(this, context.Input.Title, context.Input.Message)))
                .DisposeWith(disposables);

            viewModel.OpenFile.RegisterHandler(async context =>
            {
                await Launcher.LaunchFileInfoAsync(new FileInfo(context.Input));
                context.SetOutput(RxVoid.Default);
            }).DisposeWith(disposables);
        });

        DataContextChanged += (_, _) =>
        {
            if (DataContext is MainWindowViewModel viewModel)
            {
                SidebarColumn.Width = new GridLength(viewModel.SidebarWidth);
                RestoreWindowBounds(viewModel);
            }
        };

        Closing += (_, _) =>
        {
            try
            {
                if (WindowState == WindowState.Normal && DataContext is MainWindowViewModel viewModel)
                {
                    viewModel.SaveWindowBounds(Position.X, Position.Y, ClientSize.Width, ClientSize.Height);
                }
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

    internal ConsoleRenderer CreateConsoleRenderer() =>
        new(ScrollViewer, SelectableTextBlock, ConsoleHint);

    private void RestoreWindowBounds(MainWindowViewModel viewModel)
    {
        if (viewModel.GetWindowBounds() is not { } bounds)
        {
            return;
        }

        // Ignore stale bounds pointing at a disconnected monitor.
        var probe = new PixelPoint((int)bounds.X + VisibleCornerMargin, (int)bounds.Y + VisibleCornerMargin);
        if (!Screens.All.Any(screen => screen.Bounds.Contains(probe)))
        {
            return;
        }

        WindowStartupLocation = WindowStartupLocation.Manual;
        Position = new PixelPoint((int)bounds.X, (int)bounds.Y);
        Width = bounds.Width;
        Height = bounds.Height;
    }

    private void Console_PointerWheelChanged(object? sender, PointerWheelEventArgs e)
    {
        if (!e.KeyModifiers.HasFlag(KeyModifiers.Control)
            || DataContext is not MainWindowViewModel viewModel)
        {
            return;
        }

        viewModel.ConsoleFontSize += Math.Sign(e.Delta.Y);
        e.Handled = true;
    }

    private ColumnDefinition SidebarColumn => RootGrid.ColumnDefinitions[0];

    private void SidebarSplitter_DragCompleted(object? sender, VectorEventArgs e)
    {
        if (DataContext is MainWindowViewModel viewModel)
        {
            viewModel.SidebarWidth = SidebarColumn.ActualWidth;
        }
    }

    private void SidebarSplitter_DoubleTapped(object? sender, RoutedEventArgs e)
    {
        SidebarColumn.Width = new GridLength(MainWindowViewModel.DefaultSidebarWidth);
        if (DataContext is MainWindowViewModel viewModel)
        {
            viewModel.SidebarWidth = MainWindowViewModel.DefaultSidebarWidth;
        }
    }

    private void ConsoleThemePreset_Click(object? sender, RoutedEventArgs e)
    {
        if (sender is Button { DataContext: ConsoleTheme theme }
            && DataContext is MainWindowViewModel viewModel)
        {
            viewModel.SelectedConsoleTheme = theme;
            ConsoleThemeButton.Flyout?.Hide();
        }
    }
}
