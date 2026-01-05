using Aprillz.MewUI.Core;
using Aprillz.MewUI.Controls;

namespace Aprillz.MewUI.Platform;

public interface IPlatformHost : IDisposable
{
    IMessageBoxService MessageBox { get; }

    IWindowBackend CreateWindowBackend(Window window);

    IUiDispatcher CreateDispatcher(nint windowHandle);

    void Run(Application app, Window mainWindow);

    void Quit(Application app);

    void DoEvents();
}
