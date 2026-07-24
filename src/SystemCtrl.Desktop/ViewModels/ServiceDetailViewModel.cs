using System.Threading.Tasks;
using SystemCtrl.Core.Interfaces;
using SystemCtrl.Core.Models;
using ReactiveUI;
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

    [Reactive]
    public partial bool IsAutoStart { get; set; }

    public void Load(WindowsServiceInfo service)
    {
        Service = service;
        DetailedInfo = null;
        IsLoadingDetails = true;

        if (service != null)
        {
            IsAutoStart = service.StartType == System.ServiceProcess.ServiceStartMode.Automatic || 
                          service.StartType == System.ServiceProcess.ServiceStartMode.Boot || 
                          service.StartType == System.ServiceProcess.ServiceStartMode.System;
        }

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

    [ReactiveCommand]
    public void StartService()
    {
        if (Service == null) return;
        _windowsServiceManager.Start(Service.ServiceName);
    }

    [ReactiveCommand]
    public void StopService()
    {
        if (Service == null) return;
        _windowsServiceManager.Stop(Service.ServiceName);
    }

    [ReactiveCommand]
    public void RestartService()
    {
        if (Service == null) return;
        _windowsServiceManager.Restart(Service.ServiceName);
    }

    [ReactiveCommand]
    public void DisableAutoStart()
    {
        if (Service == null) return;
        _windowsServiceManager.SetStartType(Service.ServiceName, System.ServiceProcess.ServiceStartMode.Disabled);
        IsAutoStart = false;
        Service.StartType = System.ServiceProcess.ServiceStartMode.Disabled;
        this.RaisePropertyChanged(nameof(Service));
    }

    [ReactiveCommand]
    public void EnableAutoStart()
    {
        if (Service == null) return;
        _windowsServiceManager.SetStartType(Service.ServiceName, System.ServiceProcess.ServiceStartMode.Automatic);
        IsAutoStart = true;
        Service.StartType = System.ServiceProcess.ServiceStartMode.Automatic;
        this.RaisePropertyChanged(nameof(Service));
    }
}
