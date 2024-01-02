using Avalonia.Controls;
using Avalonia.Threading;

namespace EfGui.Actions;

public class Logger
{
    private readonly ScrollViewer _scrollViewer;
    private readonly TextBlock _textBlock;

    public Logger(ScrollViewer scrollViewer, TextBlock textBlock)
    {
        _scrollViewer = scrollViewer;
        _textBlock = textBlock;
    }

    public void LogInfo(string message) => LogInternal(message);

    private void LogInternal(string message)
    {
        Dispatcher.UIThread.InvokeAsync(() =>
        {
            _textBlock.Inlines!.Add($"{message}\r\n");
            _scrollViewer.ScrollToEnd();
        });
    }
}
