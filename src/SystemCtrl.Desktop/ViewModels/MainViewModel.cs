using System;
using System.Collections.ObjectModel;
using Microsoft.Extensions.DependencyInjection;
using ReactiveUI;
using ReactiveUI.SourceGenerators;
using SystemCtrl.Core.Interfaces;
using SystemCtrl.Core.Models;

using System.Linq;
using System.Collections.Generic;

namespace SystemCtrl.Desktop.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IWindowsServiceManager _windowsServiceManager;
    private List<WindowsServiceInfo> _allServices;

    public MainViewModel(IServiceProvider serviceProvider, IWindowsServiceManager windowsServiceManager)
    {
        _serviceProvider = serviceProvider;
        _windowsServiceManager = windowsServiceManager;

        _allServices = _windowsServiceManager.GetServices().ToList();
        Services = new ObservableCollection<WindowsServiceInfo>(_allServices);
        SlidePanel = new SlidePanelViewModel();

        this.WhenAnyValue(x => x.SearchText)
            .Subscribe(_ => FilterServices());
    }

    [Reactive]
    public partial ObservableCollection<WindowsServiceInfo> Services { get; set; }

    [Reactive]
    public partial string SearchText { get; set; } = string.Empty;

    [Reactive]
    public partial SlidePanelViewModel SlidePanel { get; set; }

    private void FilterServices()
    {
        if (string.IsNullOrWhiteSpace(SearchText))
        {
            Services = new ObservableCollection<WindowsServiceInfo>(_allServices);
        }
        else
        {
            var query = SearchText.ToLowerInvariant();
            Services = new ObservableCollection<WindowsServiceInfo>(_allServices.Where(s => 
                s.DisplayName.ToLowerInvariant().Contains(query) || 
                s.ServiceName.ToLowerInvariant().Contains(query)));
        }
    }

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

    [ReactiveCommand]
    public void RefreshServices()
    {
        _allServices = _windowsServiceManager.GetServices().ToList();
        FilterServices();
    }
}