using System;
using System.Collections.ObjectModel;
using Microsoft.Extensions.DependencyInjection;
using ReactiveUI;
using ReactiveUI.SourceGenerators;
using SystemCtrl.Core.Interfaces;
using SystemCtrl.Core.Models;

namespace SystemCtrl.Desktop.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IWindowsServiceManager _windowsServiceManager;

    public MainViewModel(IServiceProvider serviceProvider, IWindowsServiceManager windowsServiceManager)
    {
        _serviceProvider = serviceProvider;
        _windowsServiceManager = windowsServiceManager;

        Services = new ObservableCollection<WindowsServiceInfo>(_windowsServiceManager.GetServices());
        SlidePanel = new SlidePanelViewModel();
    }

    [Reactive]
    public partial ObservableCollection<WindowsServiceInfo> Services { get; set; }

    [Reactive]
    public partial SlidePanelViewModel SlidePanel { get; set; }

    [ReactiveCommand]
    public void OpenService(WindowsServiceInfo selectedService)
    {
        var serviceDetailViewModel = _serviceProvider.GetRequiredService<ServiceDetailViewModel>();
        serviceDetailViewModel.Load(selectedService);
        SlidePanel.Open(selectedService.DisplayName, serviceDetailViewModel);
    }

    [ReactiveCommand]
    public void OpenSettings()
    {
        var settingsViewModel = _serviceProvider.GetRequiredService<SettingsViewModel>();
        settingsViewModel.OnSaved = () => SlidePanel.Close();
        SlidePanel.Open("Settings", settingsViewModel);
    }
}