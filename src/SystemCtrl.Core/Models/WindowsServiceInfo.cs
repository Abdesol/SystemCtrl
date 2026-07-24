using System.ServiceProcess;

namespace SystemCtrl.Core.Models;

public class WindowsServiceInfo
{
    public string DisplayName { get; set; } = string.Empty;
    
    public string ServiceName { get; set; } = string.Empty;
    
    public string Description { get; set; } = string.Empty;

    public ServiceControllerStatus Status { get; set; } 

    public ServiceStartMode StartType { get; set; }
    
    public DetailedWindowsServiceInfo? DetailedInfo { get; set; }
}