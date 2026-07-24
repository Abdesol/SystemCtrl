using Microsoft.Extensions.DependencyInjection;
using SystemCtrl.Core;
using SystemCtrl.Core.Interfaces;
using SystemCtrl.Desktop.ViewModels;

namespace SystemCtrl.Desktop;

public static class ServiceCollectionExtensions
{
    public static void AddServices(this IServiceCollection collection)
    {
        collection.AddTransient<MainViewModel>();
        collection.AddTransient<ServiceDetailViewModel>();
        
        collection.AddScoped<IWindowsServiceManager, WindowsServiceManager>();
    }
}