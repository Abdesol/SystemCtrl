using Avalonia;
using ReactiveUI.Avalonia;
using System;
using System.Threading;

namespace SystemCtrl.Desktop;

sealed class Program
{
    private static Mutex? _mutex;

    [STAThread]
    public static void Main(string[] args)
    {
        const string appName = "SystemCtrl.Desktop.SingleInstanceMutex";
        _mutex = new Mutex(true, appName, out bool createdNew);

        if (!createdNew)
        {
            return;
        }

        BuildAvaloniaApp()
            .StartWithClassicDesktopLifetime(args);
    }

    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
#if DEBUG
            .WithDeveloperTools()
#endif
            .WithInterFont()
            .LogToTrace()
            .UseReactiveUI(_ => { });
}