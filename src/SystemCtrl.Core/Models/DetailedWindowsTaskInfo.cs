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
}
