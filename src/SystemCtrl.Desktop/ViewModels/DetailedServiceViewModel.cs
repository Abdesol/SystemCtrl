using System.Collections.ObjectModel;
using System.Threading.Tasks;
using SystemCtrl.Core.Interfaces;
using SystemCtrl.Core.Models;

namespace SystemCtrl.Desktop.ViewModels;

public class DetailedServiceViewModel : ViewModelBase
{
    private IWindowsServiceManager _windowsServiceManager;
    public DetailedServiceViewModel(IWindowsServiceManager windowsServiceManager)
    {
        _windowsServiceManager = windowsServiceManager;
    }
    
    public WindowsServiceInfo? Service { get; set; }
    public DetailedWindowsServiceInfo? DetailedInfo { get; set; }

    public async Task Load(WindowsServiceInfo service)
    {
        Service = service;
        DetailedInfo = _windowsServiceManager.GetDetailedInfo(service.ServiceName);
    }
}