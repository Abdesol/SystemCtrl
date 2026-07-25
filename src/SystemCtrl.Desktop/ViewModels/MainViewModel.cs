using System;
using System.Collections.ObjectModel;
using Microsoft.Extensions.DependencyInjection;
using ReactiveUI;
using ReactiveUI.SourceGenerators;
using SystemCtrl.Core.Interfaces;
using SystemCtrl.Core.Models;
using System.Reactive.Linq;
using System.Linq;
using System.Collections.Generic;
using SystemCtrl.Desktop.Services;

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
        SlidePanel = new SlidePanelViewModel();

        this.WhenAnyValue(x => x.SearchText)
            .Subscribe(_ => FilterServices());

        this.WhenAnyValue(x => x.SlidePanel.IsOpen)
            .Where(isOpen => !isOpen)
            .Subscribe(_ => RefreshServices());
    }

    [Reactive]
    public partial ObservableCollection<ServiceItemViewModel> Services { get; set; }

    [Reactive]
    public partial string SearchText { get; set; } = string.Empty;

    [Reactive]
    public partial SlidePanelViewModel SlidePanel { get; set; }

    private void FilterServices()
    {
        var settingsService = _serviceProvider.GetRequiredService<ISettingsService>();
        var settings = settingsService.LoadSettings();
        var pinnedServices = settings.PinnedServices;

        IEnumerable<WindowsServiceInfo> filtered = _allServices;

        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            var query = SearchText.ToLowerInvariant();
            filtered = _allServices.Where(s => 
                s.DisplayName.ToLowerInvariant().Contains(query) || 
                s.ServiceName.ToLowerInvariant().Contains(query));
        }

        var sorted = filtered.OrderByDescending(s => pinnedServices.Contains(s.ServiceName)).ThenBy(s => s.DisplayName);
        
        var viewModels = sorted.Select(s => new ServiceItemViewModel(
            s, 
            pinnedServices.Contains(s.ServiceName), 
            TogglePinAction,
            OpenServiceAction
        ));

        Services = new ObservableCollection<ServiceItemViewModel>(viewModels);
    }

    private void TogglePinAction(ServiceItemViewModel item)
    {
        var settingsService = _serviceProvider.GetRequiredService<ISettingsService>();
        var settings = settingsService.LoadSettings();
        
        if (settings.PinnedServices == null!)
            settings.PinnedServices = [];

        if (settings.PinnedServices.Contains(item.Service.ServiceName))
        {
            settings.PinnedServices.Remove(item.Service.ServiceName);
        }
        else
        {
            settings.PinnedServices.Add(item.Service.ServiceName);
        }
        
        settingsService.SaveSettings(settings);
        FilterServices();
    }

    private void OpenServiceAction(ServiceItemViewModel item)
    {
        var serviceDetailViewModel = _serviceProvider.GetRequiredService<ServiceDetailViewModel>();
        serviceDetailViewModel.Load(item.Service);
        SlidePanel.Open(item.Service.DisplayName, serviceDetailViewModel);
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