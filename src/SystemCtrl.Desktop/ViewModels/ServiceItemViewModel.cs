using System;
using System.ServiceProcess;
using ReactiveUI;
using ReactiveUI.SourceGenerators;
using SystemCtrl.Core.Models;

namespace SystemCtrl.Desktop.ViewModels;

public partial class ServiceItemViewModel : ViewModelBase
{
    private readonly Action<ServiceItemViewModel> _onTogglePin;
    private readonly Action<ServiceItemViewModel> _onOpen;

    public ServiceItemViewModel(WindowsServiceInfo service, bool isPinned, Action<ServiceItemViewModel> onTogglePin, Action<ServiceItemViewModel> onOpen)
    {
        Service = service;
        IsPinned = isPinned;
        _onTogglePin = onTogglePin;
        _onOpen = onOpen;
    }

    [Reactive]
    public partial WindowsServiceInfo Service { get; set; }

    [Reactive]
    public partial bool IsPinned { get; set; }

    public string PinActionText => IsPinned ? "Unpin" : "Pin";

    public string DisplayName => Service.DisplayName;
    public string ServiceName => Service.ServiceName;
    public string Description => Service.Description;
    public ServiceControllerStatus Status => Service.Status;
    public ServiceStartMode StartType => Service.StartType;

    [ReactiveCommand]
    public void TogglePin()
    {
        IsPinned = !IsPinned;
        this.RaisePropertyChanged(nameof(PinActionText));
        _onTogglePin?.Invoke(this);
    }

    [ReactiveCommand]
    public void OpenService()
    {
        _onOpen?.Invoke(this);
    }
}
