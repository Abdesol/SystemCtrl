using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using ReactiveUI;
using ReactiveUI.SourceGenerators;
using SystemCtrl.Core.Interfaces;
using SystemCtrl.Core.Models;

namespace SystemCtrl.Desktop.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    private readonly IServiceProvider _serviceProvider;
    private IWindowsServiceManager _windowsServiceManager;
    public MainViewModel(IServiceProvider serviceProvider, IWindowsServiceManager windowsServiceManager)
    {
        _serviceProvider = serviceProvider;
        _windowsServiceManager = windowsServiceManager;

        Services = new ObservableCollection<WindowsServiceInfo>(_windowsServiceManager.GetServices());
    }
    
    [Reactive]
    public partial ObservableCollection<WindowsServiceInfo> Services { get; set; }


    [ReactiveCommand]
    public async Task OpenService(WindowsServiceInfo selectedService)
    {
        var vm = _serviceProvider.GetRequiredService<DetailedServiceViewModel>();
        await vm.Load(selectedService);
    }
}