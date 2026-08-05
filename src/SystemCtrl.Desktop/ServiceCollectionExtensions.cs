using System.Net.Http;
using Microsoft.Extensions.DependencyInjection;
using SystemCtrl.Core;
using SystemCtrl.Core.Interfaces;
using SystemCtrl.Core.Services;
using SystemCtrl.Desktop.Services;
using SystemCtrl.Desktop.ViewModels;

namespace SystemCtrl.Desktop;

public static class ServiceCollectionExtensions
{
    public static void AddServices(this IServiceCollection collection)
    {
        collection.AddTransient<MainViewModel>();
        collection.AddTransient<ServiceDetailViewModel>();
        collection.AddTransient<TaskDetailViewModel>();
        collection.AddTransient<ErrorDialogViewModel>();

        collection.AddScoped<IAdminService, AdminService>();

        collection.AddScoped<IWindowsServiceManager, WindowsServiceManager>();
        collection.AddScoped<IWindowsTaskManager, WindowsTaskManager>();
        
        collection.AddSingleton<IElevatedResourceMonitorService, ElevatedResourceMonitorService>();
        
        collection.AddScoped<ISettingsService, SettingsService>();
        collection.AddTransient<SettingsViewModel>();

        collection.AddSingleton<HttpClient>();
        collection.AddScoped<IAiAnalyzer, AiAnalyzer>();
        collection.AddSingleton<IErrorDialogService, ErrorDialogService>();
    }
}
