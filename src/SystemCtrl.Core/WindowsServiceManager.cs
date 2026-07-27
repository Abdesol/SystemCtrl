using System.Diagnostics;
using System.Management;
using System.Security.Cryptography.X509Certificates;
using System.ServiceProcess;
using System.Text.RegularExpressions;
using Microsoft.Win32;
using SystemCtrl.Core.Interfaces;
using SystemCtrl.Core.Models;

namespace SystemCtrl.Core;

public partial class WindowsServiceManager : IWindowsServiceManager
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
    
    public DetailedWindowsServiceInfo GetDetailedInfo(string serviceName)
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
        var tempOutput = Path.GetTempFileName();
        var tempScript = Path.ChangeExtension(Path.GetTempFileName(), ".ps1");

        string wrappedArgs;
        if (fileName.Equals("powershell.exe", StringComparison.OrdinalIgnoreCase))
        {
            var innerCommand = arguments;
            if (innerCommand.StartsWith("-Command \""))
            {
                innerCommand = innerCommand.Substring(10, innerCommand.Length - 11);
            }

            string svcName = ExtractServiceName(arguments);

            var script = string.Join("\n",
                "$ErrorActionPreference = 'Stop'",
                "try {",
                "    " + innerCommand,
                "} catch {",
                "    $err = '=== Exception ===' + [Environment]::NewLine + $_.Exception.ToString()",
                "",
                "    # Walk inner exceptions",
                "    $inner = $_.Exception.InnerException",
                "    $depth = 0",
                "    while ($inner -ne $null -and $depth -lt 5) {",
                "        $err += [Environment]::NewLine + ('=== Inner Exception (level ' + ($depth+1) + ') ===') + [Environment]::NewLine + $inner.ToString()",
                "        $inner = $inner.InnerException",
                "        $depth++",
                "    }",
                "",
                "    # Recent Windows Event Log entries for this service",
                "    try {",
                $"        $svcName = '{svcName}'",
                "        $since = (Get-Date).AddSeconds(-60)",
                "        $events = Get-EventLog -LogName Application -Newest 20 -After $since -ErrorAction SilentlyContinue |",
                "            Where-Object { $_.Source -like \"*$svcName*\" -or $_.Message -like \"*$svcName*\" } |",
                "            ForEach-Object { \"[$($_.TimeGenerated)] [$($_.EntryType)] $($_.Source): \" + $_.Message.Substring(0, [Math]::Min($_.Message.Length, 800)) }",
                "        if ($events) {",
                "            $err += [Environment]::NewLine + [Environment]::NewLine + '=== Windows Application Event Log ===' + [Environment]::NewLine + ($events -join [Environment]::NewLine)",
                "        }",
                "        $sysEvents = Get-EventLog -LogName System -Newest 10 -After $since -ErrorAction SilentlyContinue |",
                "            Where-Object { $_.Source -like '*Service Control Manager*' -and $_.Message -like \"*$svcName*\" } |",
                "            ForEach-Object { \"[$($_.TimeGenerated)] [$($_.EntryType)] $($_.Source): \" + $_.Message.Substring(0, [Math]::Min($_.Message.Length, 800)) }",
                "        if ($sysEvents) {",
                "            $err += [Environment]::NewLine + [Environment]::NewLine + '=== Windows System Event Log ===' + [Environment]::NewLine + ($sysEvents -join [Environment]::NewLine)",
                "        }",
                "    } catch {}",
                "",
                $"    $err | Out-File -FilePath '{tempOutput}' -Encoding UTF8",
                "    exit 1",
                "}"
            );

            File.WriteAllText(tempScript, script, System.Text.Encoding.UTF8);
            wrappedArgs = $"-NoProfile -ExecutionPolicy Bypass -File \"{tempScript}\"";
        }
        else
        {
            wrappedArgs = $"/c {fileName} {arguments} > \"{tempOutput}\" 2>&1";
            fileName = "cmd.exe";
            tempScript = string.Empty;
        }

        var processInfo = new ProcessStartInfo
        {
            FileName = fileName,
            Arguments = wrappedArgs,
            UseShellExecute = true,
            Verb = "runas",
            WindowStyle = ProcessWindowStyle.Hidden
        };

        try
        {
            using var process = Process.Start(processInfo);
            process?.WaitForExit();

            if (process == null || process.ExitCode == 0) return;
            var output = string.Empty;
            if (File.Exists(tempOutput))
            {
                output = File.ReadAllText(tempOutput).Trim();
            }
                
            var shortMessage = "Elevated command failed.";
            if (output.Contains("System.ComponentModel.Win32Exception"))
            {
                shortMessage = "Command failed with Win32 error (Service might be disabled).";
            }
                
            throw new Exceptions.ElevatedCommandException(shortMessage, $"{fileName} {arguments}", output);
        }
        catch (System.ComponentModel.Win32Exception ex) when (ex.NativeErrorCode == 1223)
        {
            // User cancelled the UAC prompt
            return;
        }
        finally
        {
            if (File.Exists(tempOutput))
                try { File.Delete(tempOutput); }
                catch
                {
                    // ignored
                }

            if (!string.IsNullOrEmpty(tempScript) && File.Exists(tempScript))
                try { File.Delete(tempScript); }
                catch
                {
                    // ignored
                }
        }
    }
    
    private static string ExtractServiceName(string arguments)
    {
        var match = ServiceNameRegex().Match(arguments);
        return match.Success ? match.Groups[1].Value : string.Empty;
    }

    private static bool IsAccessDenied(Exception? ex)
    {
        return ex switch
        {
            null => false,
            System.ComponentModel.Win32Exception { NativeErrorCode: 5 } => true,
            _ => IsAccessDenied(ex.InnerException)
        };
    }

    public void Start(string serviceName)
    {
        using var service = new ServiceController(serviceName);

        if (service.Status == ServiceControllerStatus.Running) return;
        
        try
        {
            service.Start();
            try { service.WaitForStatus(ServiceControllerStatus.Running, TimeSpan.FromSeconds(30)); }
            catch
            {
                // ignored
            }
        }
        catch (Exception ex) when (IsAccessDenied(ex))
        {
            ExecuteElevated("powershell.exe", $"-Command \"Start-Service -Name '{serviceName}' -ErrorAction Stop\"");
        }
    }

    public void Stop(string serviceName)
    {
        using var service = new ServiceController(serviceName);

        if (service.Status == ServiceControllerStatus.Stopped) return;
        
        try
        {
            service.Stop();
            try { service.WaitForStatus(ServiceControllerStatus.Stopped, TimeSpan.FromSeconds(30)); }
            catch
            {
                // ignored
            }
        }
        catch (Exception ex) when (IsAccessDenied(ex))
        {
            ExecuteElevated("powershell.exe", $"-Command \"Stop-Service -Name '{serviceName}' -Force -ErrorAction Stop\"");
        }
    }

    public void Restart(string serviceName)
    {
        try
        {
            Stop(serviceName);
            using var service = new ServiceController(serviceName);
            service.WaitForStatus(ServiceControllerStatus.Stopped, TimeSpan.FromSeconds(30));
            service.Start();
            try { service.WaitForStatus(ServiceControllerStatus.Running, TimeSpan.FromSeconds(30)); }
            catch
            {
                // ignored
            }
        }
        catch (Exception ex) when (IsAccessDenied(ex))
        {
            ExecuteElevated("powershell.exe", $"-Command \"Restart-Service -Name '{serviceName}' -Force -ErrorAction Stop\"");
        }
    }

    public void SetStartType(string serviceName, ServiceStartMode startMode)
    {
        var startTypeStr = startMode switch
        {
            ServiceStartMode.Automatic => "auto",
            ServiceStartMode.Manual => "demand",
            ServiceStartMode.Disabled => "disabled",
            ServiceStartMode.Boot => "boot",
            ServiceStartMode.System => "system",
            _ => "demand"
        };

        var pi = new ProcessStartInfo("sc.exe", $"config \"{serviceName}\" start= {startTypeStr}") 
        { 
            CreateNoWindow = true,
            UseShellExecute = false,
            RedirectStandardError = true
        };
        
        using var process = Process.Start(pi);
        process?.WaitForExit();
        
        if (process is { ExitCode: 5 })
        {
            ExecuteElevated("sc.exe", $"config \"{serviceName}\" start= {startTypeStr}");
        }
        else if (process != null && process.ExitCode != 0)
        {
            var err = process.StandardError.ReadToEnd();
            throw new Exception($"Failed to change start type: {err}");
        }
    }

    private static bool IsSystemService(ServiceController service)
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
    
    private static string GetPublisher(string path)
    {
        if (!File.Exists(path))
            return string.Empty;

        var versionInfo = FileVersionInfo.GetVersionInfo(path);

        return versionInfo.CompanyName ?? string.Empty;
    }

    private static string GetFileVersion(string path)
    {
        if (!File.Exists(path))
            return string.Empty;

        var versionInfo = FileVersionInfo.GetVersionInfo(path);

        return versionInfo.FileVersion ?? string.Empty;
    }

    private static long? GetFileSizeBytes(string path)
    {
        if (!File.Exists(path))
            return null;

        return new FileInfo(path).Length;
    }

    
    private static bool IsSigned(string path)
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
    
    private static int? GetProcessId(ServiceController service)
    {
        using var searcher = new ManagementObjectSearcher(
            $"SELECT ProcessId FROM Win32_Service WHERE Name='{service.ServiceName}'");

        using var result = searcher.Get().Cast<ManagementObject>().FirstOrDefault();

        return result?["ProcessId"] as uint? is { } pid && pid != 0
            ? (int)pid
            : null;
    }

    private static string? GetServiceDescription(string serviceName)
    {
        using var key = Registry.LocalMachine.OpenSubKey(
            $@"SYSTEM\CurrentControlSet\Services\{serviceName}");

        return key?.GetValue("Description")?.ToString();
    }

    [GeneratedRegex("""-Name\s+['"]([^'"]+)['"]""", RegexOptions.IgnoreCase, "en-US")]
    private static partial Regex ServiceNameRegex();
}