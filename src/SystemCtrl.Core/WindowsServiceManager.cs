using System.ServiceProcess;
using Microsoft.Win32;
using SystemCtrl.Core.Interfaces;
using SystemCtrl.Core.Models;

namespace SystemCtrl.Core;

public class WindowsServiceManager : IWindowsServiceManager
{
    public IEnumerable<WindowsServiceInfo> GetServices(bool includeSystemServices = false)
    {
        return ServiceController
            .GetServices()
            .Where(service => includeSystemServices || !IsSystemService(service))
            .Select(service => new WindowsServiceInfo
            {
                Name = service.ServiceName,
                DisplayName = service.DisplayName,
                Status = service.Status.ToString(),
                StartType = service.StartType.ToString()
            });
    }

    public void Start(string serviceName)
    {
        using var service = new ServiceController(serviceName);

        if (service.Status != ServiceControllerStatus.Running)
            service.Start();
    }

    public void Stop(string serviceName)
    {
        using var service = new ServiceController(serviceName);

        if (service.Status != ServiceControllerStatus.Stopped)
            service.Stop();
    }

    public void Restart(string serviceName)
    {
        Stop(serviceName);

        using var service = new ServiceController(serviceName);
        service.WaitForStatus(ServiceControllerStatus.Stopped);

        service.Start();
    }

    private bool IsSystemService(ServiceController service)
    {
        var systemKeywords = new[]
        {
            "Microsoft Defender",
            "Windows Defender"
        };

        if (systemKeywords.Any(keyword =>
                service.ServiceName.Contains(keyword, StringComparison.OrdinalIgnoreCase) ||
                service.DisplayName.Contains(keyword, StringComparison.OrdinalIgnoreCase)))
        {
            return true;
        }
        
        using var key = Registry.LocalMachine.OpenSubKey(
            $@"SYSTEM\CurrentControlSet\Services\{service.ServiceName}");

        var path = key?.GetValue("ImagePath")?.ToString();

        if (path == null)
            return false;

        path = Environment.ExpandEnvironmentVariables(path);

        return path.StartsWith(
            Environment.GetFolderPath(Environment.SpecialFolder.Windows),
            StringComparison.OrdinalIgnoreCase);
    }
}