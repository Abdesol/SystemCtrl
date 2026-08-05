namespace SystemCtrl.Core.Models;

public class DetailedWindowsTaskInfo
{
    public string TaskName { get; set; } = string.Empty;

    public string TaskPath { get; set; } = string.Empty;

    public string[] Actions { get; set; } = [];

    public string[] Triggers { get; set; } = [];

    public string RunAsUser { get; set; } = string.Empty;

    public string CompatibilityLevel { get; set; } = string.Empty;

    public bool IsHidden { get; set; }

    public string ExecutionTimeLimit { get; set; } = string.Empty;

    public string MultipleInstancesPolicy { get; set; } = string.Empty;

    // Resource usage stats (populated only when the task is currently running)
    public int? ProcessId { get; set; }

    public double? CpuPercent { get; set; }

    public long? MemoryBytes { get; set; }

    public TimeSpan? RunningFor { get; set; }

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
