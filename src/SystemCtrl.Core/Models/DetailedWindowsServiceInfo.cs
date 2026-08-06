using ReactiveUI;
using ReactiveUI.SourceGenerators;

namespace SystemCtrl.Core.Models;

public partial class DetailedWindowsServiceInfo : ReactiveObject
{
    [Reactive] public partial string ServiceName { get; set; } = string.Empty;

    [Reactive] public partial string ExecutablePath { get; set; } = string.Empty;

    [Reactive] public partial string ExecutableName { get; set; } = string.Empty;

    [Reactive] public partial string FileVersion { get; set; } = string.Empty;

    [Reactive] public partial long? FileSizeBytes { get; set; }

    public string FileSizeFormatted => FileSizeBytes switch
    {
        null => string.Empty,
        < 1024 => $"{FileSizeBytes:N0} B",
        < 1024 * 1024 => $"{FileSizeBytes / 1024.0:N1} KB",
        _ => $"{FileSizeBytes / (1024.0 * 1024):N2} MB"
    };

    [Reactive] public partial string Publisher { get; set; } = string.Empty;

    [Reactive] public partial bool IsSigned { get; set; }

    [Reactive] public partial string ServiceAccount { get; set; } = string.Empty;

    [Reactive] public partial string[] Dependencies { get; set; } = [];

    [Reactive] public partial int? ProcessId { get; set; }

    [Reactive] public partial double? CpuPercent { get; set; }

    [Reactive] public partial long? MemoryBytes { get; set; }

    [Reactive] public partial TimeSpan? RunningFor { get; set; }

    public string MemoryFormatted => MemoryBytes switch
    {
        null => string.Empty,
        < 1024 => $"{MemoryBytes:N0} B",
        < 1024 * 1024 => $"{MemoryBytes / 1024.0:N1} KB",
        < 1024L * 1024 * 1024 => $"{MemoryBytes / (1024.0 * 1024):N1} MB",
        _ => $"{MemoryBytes / (1024.0 * 1024 * 1024):N2} GB"
    };

    public string RunningForFormatted
    {
        get
        {
            if (RunningFor is not { } ts) return string.Empty;
            if (ts.TotalHours >= 1)
                return $"{(int)ts.TotalHours}h {ts.Minutes:D2}m {ts.Seconds:D2}s";
            if (ts.TotalMinutes >= 1)
                return $"{ts.Minutes}m {ts.Seconds:D2}s";
            return $"{ts.Seconds}s";
        }
    }
}