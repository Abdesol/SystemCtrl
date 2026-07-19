using System.Collections.ObjectModel;
using ReactiveUI;
using ReactiveUI.SourceGenerators;
using SystemCtrl.Core.Interfaces;
using SystemCtrl.Core.Models;

namespace SystemCtrl.Desktop.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    private IWindowsServiceManager _windowsServiceManager;
    public MainViewModel(IWindowsServiceManager windowsServiceManager)
    {
        _windowsServiceManager = windowsServiceManager;

        Services = new ObservableCollection<WindowsServiceInfo>(_windowsServiceManager.GetServices());
    }
    
    [Reactive]
    public partial ObservableCollection<WindowsServiceInfo> Services { get; set; }
}