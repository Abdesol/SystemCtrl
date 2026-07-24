using System.Diagnostics;
using System.Management;
using System.Security.Cryptography.X509Certificates;
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
                DisplayName = service.DisplayName,
                ServiceName = service.ServiceName.ToUpper(),
                Description = GetServiceDescription(service.ServiceName),
                Status = service.Status,
                StartType = service.StartType
            });
    }
    
    public DetailedWindowsServiceInfo? GetDetailedInfo(string serviceName)
    {
        using var service = new ServiceController(serviceName);

        using var key = Registry.LocalMachine.OpenSubKey(
            $@"SYSTEM\CurrentControlSet\Services\{serviceName}");

        var path = key?.GetValue("ImagePath")?.ToString() ?? string.Empty;

        path = Environment.ExpandEnvironmentVariables(path)
            .Trim('"');

        var executableName = string.IsNullOrWhiteSpace(path)
            ? string.Empty
            : Path.GetFileName(path);

        return new DetailedWindowsServiceInfo
        {
            ServiceName = service.ServiceName,
            ExecutablePath = path,
            ExecutableName = executableName,
            Publisher = GetPublisher(path),
            IsSigned = IsSigned(path),
            ServiceAccount = key?.GetValue("ObjectName")?.ToString() ?? string.Empty,
            StartupAccount = key?.GetValue("ObjectName")?.ToString() ?? string.Empty,
            ProcessId = GetProcessId(service),
            // Category = DetermineCategory(service, path)
        };
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
    
    private string GetPublisher(string path)
    {
        if (!File.Exists(path))
            return string.Empty;

        var versionInfo = FileVersionInfo.GetVersionInfo(path);

        return versionInfo.CompanyName ?? string.Empty;
    }
    
    private bool IsSigned(string path)
    {
        if (!File.Exists(path))
            return false;

        try
        {
            _ = X509CertificateLoader.LoadCertificateFromFile(path);
            return true;
        }
        catch
        {
            return false;
        }
    }
    
    private int? GetProcessId(ServiceController service)
    {
        using var searcher = new ManagementObjectSearcher(
            $"SELECT ProcessId FROM Win32_Service WHERE Name='{service.ServiceName}'");

        using var result = searcher.Get().Cast<ManagementObject>().FirstOrDefault();

        return result?["ProcessId"] as uint? is { } pid
            ? (int)pid
            : null;
    }
    
    public static string? GetServiceDescription(string serviceName)
    {
        using var key = Registry.LocalMachine.OpenSubKey(
            $@"SYSTEM\CurrentControlSet\Services\{serviceName}");

        return key?.GetValue("Description")?.ToString();
    }
}