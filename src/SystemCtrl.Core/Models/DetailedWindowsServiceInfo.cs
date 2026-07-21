namespace SystemCtrl.Core.Models;

public class DetailedWindowsServiceInfo
{
    public string ServiceName { get; set; } = string.Empty;
    
    public string ExecutablePath { get; set; } = string.Empty;
    
    public string ExecutableName { get; set; } = string.Empty;

    public string Publisher { get; set; } = string.Empty;
    
    public bool IsSigned { get; set; }

    public string ServiceAccount { get; set; } = string.Empty;
    
    public string StartupAccount { get; set; } = string.Empty;

    public string Category { get; set; } = string.Empty;
    
    public DateTime? InstalledDate { get; set; }

    public int? ProcessId { get; set; }
}