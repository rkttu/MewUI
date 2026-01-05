namespace Aprillz.MewUI.Platform;

public interface IUiDispatcher
{
    bool IsOnUIThread { get; }

    void Post(Action action);

    void Send(Action action);

    void ProcessWorkItems();
}

