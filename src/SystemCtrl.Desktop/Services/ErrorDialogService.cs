using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Threading;
using SystemCtrl.Desktop.Views;

namespace SystemCtrl.Desktop.Services;

public class ErrorDialogService : IErrorDialogService
{
    public Task ShowAsync(string title, string message, string detail)
    {
        return Dispatcher.UIThread.InvokeAsync(async () =>
        {
            var owner = GetMainWindow();

            var dialog = new ErrorDialogWindow();
            dialog.SetContent(title, message, detail);

            if (owner != null)
                await dialog.ShowDialog(owner);
            else
                dialog.Show();
        });
    }

    private static Window? GetMainWindow()
    {
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            return desktop.MainWindow;
        return null;
    }
}
