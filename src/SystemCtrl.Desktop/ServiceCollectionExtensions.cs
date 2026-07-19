using Microsoft.Extensions.DependencyInjection;
using SystemCtrl.Desktop.ViewModels;

namespace SystemCtrl.Desktop;

public static class ServiceCollectionExtensions
{
    public static void AddServices(this IServiceCollection collection)
    {
        collection.AddTransient<MainViewModel>();
    }
}