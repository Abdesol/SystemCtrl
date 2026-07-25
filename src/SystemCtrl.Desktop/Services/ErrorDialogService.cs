using System;
using System.Threading.Tasks;
using Avalonia.Input.Platform;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Threading;
using SystemCtrl.Desktop.Views;
using SystemCtrl.Desktop.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace SystemCtrl.Desktop.Services;

public class ErrorDialogService : IErrorDialogService
{
    private readonly IServiceProvider _serviceProvider;

    public ErrorDialogService(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public Task ShowAsync(string title, string message, string detail)
    {
        return Dispatcher.UIThread.InvokeAsync(async () =>
        {
            var owner = GetMainWindow();

            var dialog = new ErrorDialogWindow();
            var vm = _serviceProvider.GetRequiredService<ErrorDialogViewModel>();
            
            vm.Init(title, message, detail, 
                closeAction: () => dialog.Close(),
                copyAction: async (text) => 
                {
                    var clipboard = TopLevel.GetTopLevel(dialog)?.Clipboard;
                    if (clipboard != null)
                    {
                        await clipboard.SetTextAsync(text);
                    }
                });

            dialog.DataContext = vm;

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
