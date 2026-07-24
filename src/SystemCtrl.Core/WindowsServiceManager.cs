using System.Diagnostics;
using System.Management;
using System.Security.Cryptography.X509Certificates;
using System.Security.Principal;
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
                Description = GetServiceDescription(service.ServiceName) ?? "(No Description)",
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

        path = Environment.ExpandEnvironmentVariables(path).Trim('"');

        var executableName = string.IsNullOrWhiteSpace(path)
            ? string.Empty
            : Path.GetFileName(path);

        var dependencies = service.ServicesDependedOn
            .Select(d => d.DisplayName)
            .ToArray();

        return new DetailedWindowsServiceInfo
        {
            ServiceName = service.ServiceName,
            ExecutablePath = path,
            ExecutableName = executableName,
            FileVersion = GetFileVersion(path),
            FileSizeBytes = GetFileSizeBytes(path),
            Publisher = GetPublisher(path),
            IsSigned = IsSigned(path),
            ServiceAccount = key?.GetValue("ObjectName")?.ToString() ?? string.Empty,
            Dependencies = dependencies,
            ProcessId = GetProcessId(service),
        };
    }

    private void ExecuteElevated(string fileName, string arguments)
    {
        var processInfo = new ProcessStartInfo
        {
            FileName = fileName,
            Arguments = arguments,
            UseShellExecute = true,
            Verb = "runas",
            WindowStyle = ProcessWindowStyle.Hidden
        };

        try
        {
            using var process = Process.Start(processInfo);
            process?.WaitForExit();
        }
        catch (System.ComponentModel.Win32Exception)
        {
            // User cancelled the UAC prompt
        }
    }

    private bool IsAdministrator()
    {
        using (WindowsIdentity identity = WindowsIdentity.GetCurrent())
        {
            WindowsPrincipal principal = new WindowsPrincipal(identity);
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }
    }

    public void Start(string serviceName)
    {
        using var service = new ServiceController(serviceName);

        if (service.Status != ServiceControllerStatus.Running)
        {
            if (IsAdministrator())
            {
                service.Start();
            }
            else
            {
                ExecuteElevated("sc.exe", $"start \"{serviceName}\"");
            }
        }
    }

    public void Stop(string serviceName)
    {
        using var service = new ServiceController(serviceName);

        if (service.Status != ServiceControllerStatus.Stopped)
        {
            if (IsAdministrator())
            {
                service.Stop();
            }
            else
            {
                ExecuteElevated("sc.exe", $"stop \"{serviceName}\"");
            }
        }
    }

    public void Restart(string serviceName)
    {
        if (IsAdministrator())
        {
            Stop(serviceName);
            using var service = new ServiceController(serviceName);
            service.WaitForStatus(ServiceControllerStatus.Stopped);
            service.Start();
        }
        else
        {
            ExecuteElevated("powershell.exe", $"-Command \"Restart-Service -Name '{serviceName}' -Force\"");
        }
    }

    public void SetStartType(string serviceName, ServiceStartMode startMode)
    {
        string startTypeStr = startMode switch
        {
            ServiceStartMode.Automatic => "auto",
            ServiceStartMode.Manual => "demand",
            ServiceStartMode.Disabled => "disabled",
            ServiceStartMode.Boot => "boot",
            ServiceStartMode.System => "system",
            _ => "demand"
        };

        if (IsAdministrator())
        {
             var pi = new ProcessStartInfo("sc.exe", $"config \"{serviceName}\" start= {startTypeStr}") { CreateNoWindow = true };
             Process.Start(pi)?.WaitForExit();
        }
        else
        {
             ExecuteElevated("sc.exe", $"config \"{serviceName}\" start= {startTypeStr}");
        }
    }

    private bool IsSystemService(ServiceController service)
    {
        var systemKeywords = new[]
        {
            "Microsoft Defender",
            "Windows Defender",
            "Windows Media"
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

        if (string.IsNullOrWhiteSpace(path))
            return false;

        path = Environment.ExpandEnvironmentVariables(path).Trim('"');

        if (path.StartsWith(@"\SystemRoot\", StringComparison.OrdinalIgnoreCase))
        {
            path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), path.Substring(@"\SystemRoot\".Length));
        }
        else if (path.StartsWith(@"System32\", StringComparison.OrdinalIgnoreCase))
        {
            path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), path.Substring(@"System32\".Length));
        }
        else if (path.StartsWith(@"\??\"))
        {
            path = path.Substring(@"\??\".Length);
        }

        if (path.Contains(@"\system32\", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

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

    private string GetFileVersion(string path)
    {
        if (!File.Exists(path))
            return string.Empty;

        var versionInfo = FileVersionInfo.GetVersionInfo(path);

        return versionInfo.FileVersion ?? string.Empty;
    }

    private long? GetFileSizeBytes(string path)
    {
        if (!File.Exists(path))
            return null;

        return new FileInfo(path).Length;
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

        return result?["ProcessId"] as uint? is { } pid && pid != 0
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