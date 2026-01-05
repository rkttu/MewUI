using System.Collections.Concurrent;

using Aprillz.MewUI.Native;
using Aprillz.MewUI.Native.Constants;

namespace Aprillz.MewUI.Platform.Win32;

public sealed class Win32UiDispatcher : SynchronizationContext, IUiDispatcher
{
    private readonly ConcurrentQueue<WorkItem> _workItems = new();
    private readonly nint _hwnd;
    private readonly int _mainThreadId;

    internal const uint WM_INVOKE = WindowMessages.WM_USER + 1;

    private readonly struct WorkItem
    {
        public readonly SendOrPostCallback Callback;
        public readonly object? State;
        public readonly ManualResetEventSlim? Signal;

        public WorkItem(SendOrPostCallback callback, object? state, ManualResetEventSlim? signal = null)
        {
            Callback = callback;
            State = state;
            Signal = signal;
        }
    }

    internal Win32UiDispatcher(nint hwnd)
    {
        _hwnd = hwnd;
        _mainThreadId = Environment.CurrentManagedThreadId;
    }

    public bool IsOnUIThread => Environment.CurrentManagedThreadId == _mainThreadId;

    public override void Post(SendOrPostCallback d, object? state)
    {
        ArgumentNullException.ThrowIfNull(d);
        _workItems.Enqueue(new WorkItem(d, state));
        User32.PostMessage(_hwnd, WM_INVOKE, 0, 0);
    }

    public override void Send(SendOrPostCallback d, object? state)
    {
        ArgumentNullException.ThrowIfNull(d);

        if (IsOnUIThread)
        {
            d(state);
            return;
        }

        using var signal = new ManualResetEventSlim(false);
        _workItems.Enqueue(new WorkItem(d, state, signal));
        User32.PostMessage(_hwnd, WM_INVOKE, 0, 0);
        signal.Wait();
    }

    public void Post(Action action)
    {
        ArgumentNullException.ThrowIfNull(action);
        Post(_ => action(), null);
    }

    public void Send(Action action)
    {
        ArgumentNullException.ThrowIfNull(action);
        Send(_ => action(), null);
    }

    public void ProcessWorkItems()
    {
        while (_workItems.TryDequeue(out var item))
        {
            try
            {
                item.Callback(item.State);
            }
            finally
            {
                item.Signal?.Set();
            }
        }
    }
}

