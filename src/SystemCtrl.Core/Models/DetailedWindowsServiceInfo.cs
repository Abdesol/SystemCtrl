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
}