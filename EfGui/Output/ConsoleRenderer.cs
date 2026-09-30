using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Media;
using Avalonia.Threading;
using EfGui.Core.Services;
using System.Collections.Concurrent;

namespace EfGui.Output;

public sealed class ConsoleRenderer : IConsole
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

    // Lines arrive from process output threads and are drained in one UI pass, so a
    // chatty build costs one layout and scroll per batch instead of per line.
    // A null entry is a Clear, queued so it stays ordered with the writes around it.
    private readonly ConcurrentQueue<(ConsoleMessageKind Kind, string Text)?> _pending = new();
    private int _flushScheduled;

    public ConsoleRenderer(ScrollViewer scrollViewer, SelectableTextBlock textBlock, Control emptyHint)
    {
        _scrollViewer = scrollViewer;
        _textBlock = textBlock;
        _emptyHint = emptyHint;
    }

    public void WriteLine(ConsoleMessageKind kind, string text) => Enqueue((kind, text));

    public void Clear() => Enqueue(null);

    private void Enqueue((ConsoleMessageKind Kind, string Text)? item)
    {
        _pending.Enqueue(item);
        if (Interlocked.Exchange(ref _flushScheduled, 1) == 0)
            Dispatcher.UIThread.Post(Flush, DispatcherPriority.Background);
    }

    private void Flush()
    {
        // Reset before draining: anything enqueued from here on schedules another flush.
        Volatile.Write(ref _flushScheduled, 0);

        var inlines = _textBlock.Inlines!;
        var batch = new List<Inline>();
        while (_pending.TryDequeue(out var item))
        {
            if (item is { } line)
            {
                batch.Add(CreateRun(line.Kind, line.Text));
                continue;
            }

            batch.Clear();
            inlines.Clear();
        }

        if (batch.Count > 0)
        {
            inlines.AddRange(batch);
            if (inlines.Count >= MaxLines + TrimBatch)
                inlines.RemoveRange(0, inlines.Count - MaxLines);
            _scrollViewer.ScrollToEnd();
        }

        _emptyHint.IsVisible = inlines.Count == 0;
    }

    private static Run CreateRun(ConsoleMessageKind kind, string text)
    {
        var run = new Run(text + "\n") { Foreground = BrushFor(kind) };
        if (kind == ConsoleMessageKind.Command)
            run.FontWeight = FontWeight.Bold;
        return run;
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
