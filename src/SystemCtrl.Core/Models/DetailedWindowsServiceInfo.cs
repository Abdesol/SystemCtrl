namespace SystemCtrl.Core.Models;

public class DetailedWindowsServiceInfo
{
    public string ServiceName { get; set; } = string.Empty;

    public string ExecutablePath { get; set; } = string.Empty;

    public string ExecutableName { get; set; } = string.Empty;

    public string FileVersion { get; set; } = string.Empty;

    public long? FileSizeBytes { get; set; }

    public string FileSizeFormatted => FileSizeBytes switch
    {
        null => string.Empty,
        < 1024 => $"{FileSizeBytes:N0} B",
        < 1024 * 1024 => $"{FileSizeBytes / 1024.0:N1} KB",
        _ => $"{FileSizeBytes / (1024.0 * 1024):N2} MB"
    };

    public string Publisher { get; set; } = string.Empty;

    public bool IsSigned { get; set; }

    public string ServiceAccount { get; set; } = string.Empty;

    public string[] Dependencies { get; set; } = [];

    public int? ProcessId { get; set; }
}