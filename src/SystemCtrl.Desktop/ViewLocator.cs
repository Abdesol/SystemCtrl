using Avalonia.Controls;
using Avalonia.Controls.Templates;
using SystemCtrl.Desktop.ViewModels;
using SystemCtrl.Desktop.Views;

namespace SystemCtrl.Desktop;

public class ViewLocator : IDataTemplate
{
    public Control? Build(object? param)
    {
        if (param is null)
            return null;

        return param switch
        {
            MainViewModel => new MainWindow(),
            ErrorDialogViewModel => new ErrorDialogWindow(),
            ServiceDetailViewModel => new ServiceDetailView(),
            TaskDetailViewModel => new TaskDetailView(),
            SettingsViewModel => new SettingsView(),
            
            _ => new TextBlock { Text = "Not Found: " + param.GetType().FullName }
        };
    }

    public bool Match(object? data)
    {
        return data is ViewModelBase;
    }
}