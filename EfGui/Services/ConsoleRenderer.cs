using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using Avalonia.Threading;
using EfGui.Core;
using System.Collections.Concurrent;

namespace EfGui.Services;

public sealed class ConsoleRenderer : IConsole
{
    private static readonly IBrush CommandBrush = new ImmutableSolidColorBrush(Color.Parse("#FFFFFF"));
    private static readonly IBrush StdOutBrush = new ImmutableSolidColorBrush(Color.Parse("#C7D5E8"));
    private static readonly IBrush StdErrBrush = new ImmutableSolidColorBrush(Color.Parse("#FFB199"));
    private static readonly IBrush InfoBrush = new ImmutableSolidColorBrush(Color.Parse("#8FB8E8"));
    private static readonly IBrush SuccessBrush = new ImmutableSolidColorBrush(Color.Parse("#9CE29C"));
    private static readonly IBrush ErrorBrush = new ImmutableSolidColorBrush(Color.Parse("#FF8080"));

    private const int MaxLines = 5000;
    private const int TrimBatch = 500;

    private readonly ScrollViewer _scrollViewer;
    private readonly SelectableTextBlock _textBlock;
    private readonly Control _emptyHint;

    // Drained in batches on the UI thread; null means Clear, queued to keep ordering.
    private readonly ConcurrentQueue<PendingLine?> _pending = new();
    private int _flushScheduled;

    public ConsoleRenderer(ScrollViewer scrollViewer, SelectableTextBlock textBlock, Control emptyHint)
    {
        _scrollViewer = scrollViewer;
        _textBlock = textBlock;
        _emptyHint = emptyHint;
    }

    public void WriteLine(ConsoleMessageKind kind, string text) => Enqueue(new PendingLine(kind, text));

    public void Clear() => Enqueue(null);

    private void Enqueue(PendingLine? item)
    {
        _pending.Enqueue(item);
        if (Interlocked.Exchange(ref _flushScheduled, 1) == 0)
        {
            Dispatcher.UIThread.Post(Flush, DispatcherPriority.Background);
        }
    }

    private void Flush()
    {
        // Reset before draining: anything enqueued from here on schedules another flush.
        Volatile.Write(ref _flushScheduled, 0);

        var inlines = _textBlock.Inlines!;
        var batch = new List<PendingLine>();
        while (_pending.TryDequeue(out var item))
        {
            if (item is { } line)
            {
                batch.Add(line);
                continue;
            }

            batch.Clear();
            inlines.Clear();
        }

        if (batch.Count > 0)
        {
            var atBottom = _scrollViewer.Offset.Y >= _scrollViewer.Extent.Height - _scrollViewer.Viewport.Height - 1;

            inlines.AddRange(batch.Skip(Math.Max(0, batch.Count - MaxLines)).Select(CreateRun));
            if (inlines.Count >= MaxLines + TrimBatch)
            {
                inlines.RemoveRange(0, inlines.Count - MaxLines);
            }

            if (atBottom)
            {
                _scrollViewer.ScrollToEnd();
            }
        }

        _emptyHint.IsVisible = inlines.Count == 0;
    }

    private static Run CreateRun(PendingLine line)
    {
        var run = new Run(line.Text + "\n") { Foreground = BrushFor(line.Kind) };
        if (line.Kind == ConsoleMessageKind.Command)
        {
            run.FontWeight = FontWeight.Bold;
        }

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

    private readonly record struct PendingLine(ConsoleMessageKind Kind, string Text);
}
