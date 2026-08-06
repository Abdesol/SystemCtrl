using System;

namespace SystemCtrl.Core.Models;

public class ResourceStatsUpdate
{
    public int ProcessId { get; set; }
    public double? CpuPercent { get; set; }
    public long? MemoryBytes { get; set; }
    public TimeSpan? RunningFor { get; set; }
    public string? Error { get; set; }
}
