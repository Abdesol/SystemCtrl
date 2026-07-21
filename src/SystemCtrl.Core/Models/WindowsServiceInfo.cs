namespace SystemCtrl.Core.Models;

public class WindowsServiceInfo
{
    public string Name { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public string StartType { get; set; } = string.Empty;
    
    public DetailedWindowsServiceInfo? DetailedInfo { get; set; }
}