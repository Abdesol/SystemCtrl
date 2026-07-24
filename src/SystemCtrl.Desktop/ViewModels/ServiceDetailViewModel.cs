using System.Threading.Tasks;
using SystemCtrl.Core.Interfaces;
using SystemCtrl.Core.Models;
using ReactiveUI.SourceGenerators;

namespace SystemCtrl.Desktop.ViewModels;

public partial class ServiceDetailViewModel : ViewModelBase
{
    private readonly IWindowsServiceManager _windowsServiceManager;

    public ServiceDetailViewModel(IWindowsServiceManager windowsServiceManager)
    {
        _windowsServiceManager = windowsServiceManager;
    }

    [Reactive]
    public partial WindowsServiceInfo? Service { get; set; }

    [Reactive]
    public partial DetailedWindowsServiceInfo? DetailedInfo { get; set; }

    [Reactive]
    public partial bool IsLoadingDetails { get; set; }

    public void Load(WindowsServiceInfo service)
    {
        Service = service;
        DetailedInfo = null;
        IsLoadingDetails = true;

        Task.Run(() =>
        {
            try
            {
                return _windowsServiceManager.GetDetailedInfo(service.ServiceName);
            }
            catch
            {
                return null;
            }
        }).ContinueWith(t =>
        {
            DetailedInfo = t.Result;
            IsLoadingDetails = false;
        }, TaskScheduler.FromCurrentSynchronizationContext());
    }
}
