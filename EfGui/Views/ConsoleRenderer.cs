using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Media;
using Avalonia.Threading;
using EfGui.Services;

namespace EfGui.Views;

public class ConsoleRenderer : IConsole
{
    private static readonly IBrush CommandBrush = new SolidColorBrush(Color.Parse("#FFFFFF"));
    private static readonly IBrush StdOutBrush = new SolidColorBrush(Color.Parse("#C7D5E8"));
    private static readonly IBrush StdErrBrush = new SolidColorBrush(Color.Parse("#FFB199"));
    private static readonly IBrush InfoBrush = new SolidColorBrush(Color.Parse("#8FB8E8"));
    private static readonly IBrush SuccessBrush = new SolidColorBrush(Color.Parse("#9CE29C"));
    private static readonly IBrush ErrorBrush = new SolidColorBrush(Color.Parse("#FF8080"));

    // Bound the number of lines kept so long sessions don't grow memory or slow
    // rendering. Trim in batches to avoid per-line removal churn.
    private const int MaxLines = 5000;
    private const int TrimBatch = 500;

    private readonly ScrollViewer _scrollViewer;
    private readonly SelectableTextBlock _textBlock;
    private readonly Control _emptyHint;

    public ConsoleRenderer(ScrollViewer scrollViewer, SelectableTextBlock textBlock, Control emptyHint)
    {
        _scrollViewer = scrollViewer;
        _textBlock = textBlock;
        _emptyHint = emptyHint;
    }

    public void WriteLine(ConsoleMessageKind kind, string text)
    {
        Dispatcher.UIThread.InvokeAsync(() =>
        {
            _emptyHint.IsVisible = false;

            var run = new Run(text + "\n") { Foreground = BrushFor(kind) };
            if (kind == ConsoleMessageKind.Command)
                run.FontWeight = FontWeight.Bold;

            var inlines = _textBlock.Inlines!;
            if (inlines.Count >= MaxLines + TrimBatch)
            {
                for (var i = 0; i < TrimBatch; i++)
                    inlines.RemoveAt(0);
            }

            inlines.Add(run);
            _scrollViewer.ScrollToEnd();
        });
    }

    public void Clear()
    {
        Dispatcher.UIThread.InvokeAsync(() =>
        {
            _textBlock.Inlines!.Clear();
            _emptyHint.IsVisible = true;
        });
    }

    private static IBrush BrushFor(ConsoleMessageKind kind) => kind switch
    {
        ConsoleMessageKind.Command => CommandBrush,
        ConsoleMessageKind.StdErr => StdErrBrush,
        ConsoleMessageKind.Info => InfoBrush,
        ConsoleMessageKind.Success => SuccessBrush,
        ConsoleMessageKind.Error => ErrorBrush,
        _ => StdOutBrush
    };
}
