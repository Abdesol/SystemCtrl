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
    private readonly Action<ServiceItemViewModel> _onStart;
    private readonly Action<ServiceItemViewModel> _onStop;

    public ServiceItemViewModel(WindowsServiceInfo service, bool isPinned, Action<ServiceItemViewModel> onTogglePin, Action<ServiceItemViewModel> onOpen, Action<ServiceItemViewModel> onStart, Action<ServiceItemViewModel> onStop)
    {
        Service = service;
        IsPinned = isPinned;
        _onTogglePin = onTogglePin;
        _onOpen = onOpen;
        _onStart = onStart;
        _onStop = onStop;
    }

    [Reactive]
    public partial WindowsServiceInfo Service { get; set; }

    [Reactive]
    public partial bool IsPinned { get; set; }

    [Reactive]
    public partial bool IsBusy { get; set; }

    public string PinActionText => IsPinned ? "Unpin" : "Pin";

    public string DisplayName => Service.DisplayName;
    public string ServiceName => Service.ServiceName;
    public string Description => Service.Description;
    public ServiceControllerStatus Status => Service.Status;
    public ServiceStartMode StartType => Service.StartType;

    public bool CanStart => Status == ServiceControllerStatus.Stopped && StartType != ServiceStartMode.Disabled;
    public bool CanStop => Status == ServiceControllerStatus.Running;

    [ReactiveCommand]
    public void TogglePinCommand()
    {
        IsPinned = !IsPinned;
        this.RaisePropertyChanged(nameof(PinActionText));
        _onTogglePin?.Invoke(this);
    }

    [ReactiveCommand]
    public void OpenServiceCommand()
    {
        _onOpen?.Invoke(this);
    }

    [ReactiveCommand]
    public void StartCommand()
    {
        _onStart?.Invoke(this);
    }

    [ReactiveCommand]
    public void StopCommand()
    {
        _onStop?.Invoke(this);
    }
}
