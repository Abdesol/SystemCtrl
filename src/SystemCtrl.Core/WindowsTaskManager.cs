using System.Diagnostics;
using Microsoft.Win32.TaskScheduler;
using TsTask = Microsoft.Win32.TaskScheduler.Task;
using SystemCtrl.Core.Exceptions;
using SystemCtrl.Core.Interfaces;
using SystemCtrl.Core.Models;

namespace SystemCtrl.Core;

public partial class WindowsTaskManager : IWindowsTaskManager
{
    private static readonly string[] SystemTaskFolders =
    [
        @"\Microsoft\Windows\"
    ];

    public IEnumerable<WindowsTaskInfo> GetTasks(bool includeSystemTasks = false)
    {
        using var ts = new TaskService();
        return EnumerateTasks(ts.RootFolder, includeSystemTasks).ToList();
    }

    private static IEnumerable<WindowsTaskInfo> EnumerateTasks(TaskFolder folder, bool includeSystemTasks)
    {
        foreach (var task in folder.Tasks)
        {
            if (!includeSystemTasks && IsSystemTask(task))
                continue;

            yield return MapToInfo(task);
        }

        foreach (var sub in folder.SubFolders)
        {
            foreach (var task in EnumerateTasks(sub, includeSystemTasks))
                yield return task;
        }
    }

    private static WindowsTaskInfo MapToInfo(TsTask task)
    {
        var def = task.Definition;
        var regInfo = def.RegistrationInfo;

        DateTime? nextRun = null;
        try { nextRun = task.NextRunTime == DateTime.MinValue ? null : task.NextRunTime; }
        catch { /* ignored */ }

        DateTime? lastRun = null;
        try { lastRun = task.LastRunTime == DateTime.MinValue ? null : task.LastRunTime; }
        catch { /* ignored */ }

        return new WindowsTaskInfo
        {
            TaskName = task.Name,
            TaskPath = task.Path,
            Description = regInfo.Description ?? string.Empty,
            Author = regInfo.Author ?? string.Empty,
            Status = task.State,
            TriggerSummary = BuildTriggerSummary(def.Triggers),
            NextRunTime = nextRun,
            LastRunTime = lastRun,
            LastRunResult = task.LastTaskResult,
            RunAsUser = def.Principal.UserId ?? string.Empty
        };
    }

    private static string BuildTriggerSummary(TriggerCollection triggers)
    {
        if (triggers.Count == 0)
            return "No trigger";

        var t = triggers[0];
        return t switch
        {
            DailyTrigger d => $"Daily at {d.StartBoundary:HH:mm}",
            WeeklyTrigger w => $"Weekly on {w.DaysOfWeek} at {w.StartBoundary:HH:mm}",
            MonthlyTrigger => $"Monthly at {t.StartBoundary:HH:mm}",
            TimeTrigger => $"Once at {t.StartBoundary:g}",
            BootTrigger => "At system startup",
            LogonTrigger => "At logon",
            IdleTrigger => "On idle",
            EventTrigger => "On event",
            _ => t.TriggerType.ToString()
        };
    }

    public DetailedWindowsTaskInfo GetDetailedInfo(string taskPath)
    {
        using var ts = new TaskService();
        var task = ts.GetTask(taskPath);

        if (task == null)
            return new DetailedWindowsTaskInfo { TaskPath = taskPath };

        var def = task.Definition;

        var actions = def.Actions
            .Select(a => a switch
            {
                ExecAction ea => string.IsNullOrWhiteSpace(ea.Arguments)
                    ? ea.Path
                    : $"{ea.Path} {ea.Arguments}",
                _ => a.ActionType.ToString()
            })
            .ToArray();

        var triggers = def.Triggers
            .Select(BuildDetailedTrigger)
            .ToArray();

        var timeLimit = def.Settings.ExecutionTimeLimit == TimeSpan.Zero
            ? "No limit"
            : def.Settings.ExecutionTimeLimit.ToString();

        return new DetailedWindowsTaskInfo
        {
            TaskName = task.Name,
            TaskPath = task.Path,
            Actions = actions,
            Triggers = triggers,
            RunAsUser = def.Principal.UserId ?? string.Empty,
            CompatibilityLevel = def.Settings.Compatibility.ToString(),
            IsHidden = def.Settings.Hidden,
            ExecutionTimeLimit = timeLimit,
            MultipleInstancesPolicy = def.Settings.MultipleInstances.ToString()
        };
    }

    private static string BuildDetailedTrigger(Trigger t)
    {
        var enabled = t.Enabled ? string.Empty : " (disabled)";
        return t switch
        {
            DailyTrigger d => $"Daily every {d.DaysInterval} day(s) at {d.StartBoundary:HH:mm}{enabled}",
            WeeklyTrigger w => $"Weekly on {w.DaysOfWeek} at {w.StartBoundary:HH:mm}{enabled}",
            MonthlyTrigger m => $"Monthly at {m.StartBoundary:HH:mm}{enabled}",
            TimeTrigger => $"Once at {t.StartBoundary:g}{enabled}",
            BootTrigger => $"At system startup{enabled}",
            LogonTrigger l => string.IsNullOrEmpty(l.UserId)
                ? $"At logon (any user){enabled}"
                : $"At logon of {l.UserId}{enabled}",
            IdleTrigger => $"On idle{enabled}",
            EventTrigger => $"On event{enabled}",
            _ => $"{t.TriggerType}{enabled}"
        };
    }

    public void Enable(string taskPath)
    {
        try
        {
            using var ts = new TaskService();
            var task = ts.GetTask(taskPath);
            if (task == null) return;
            task.Enabled = true;
        }
        catch (UnauthorizedAccessException)
        {
            ExecuteElevated("schtasks.exe", $"/Change /TN \"{taskPath}\" /Enable");
        }
    }

    public void Disable(string taskPath)
    {
        try
        {
            using var ts = new TaskService();
            var task = ts.GetTask(taskPath);
            if (task == null) return;
            task.Enabled = false;
        }
        catch (UnauthorizedAccessException)
        {
            ExecuteElevated("schtasks.exe", $"/Change /TN \"{taskPath}\" /Disable");
        }
    }

    public void Run(string taskPath)
    {
        try
        {
            using var ts = new TaskService();
            var task = ts.GetTask(taskPath);
            task?.Run();
        }
        catch (UnauthorizedAccessException)
        {
            ExecuteElevated("schtasks.exe", $"/Run /TN \"{taskPath}\"");
        }
    }

    public void Stop(string taskPath)
    {
        try
        {
            using var ts = new TaskService();
            var task = ts.GetTask(taskPath);
            task?.Stop();
        }
        catch (UnauthorizedAccessException)
        {
            ExecuteElevated("schtasks.exe", $"/End /TN \"{taskPath}\"");
        }
    }

    private static bool IsSystemTask(TsTask task)
    {
        return SystemTaskFolders.Any(f =>
            task.Path.StartsWith(f, StringComparison.OrdinalIgnoreCase));
    }

    private void ExecuteElevated(string fileName, string arguments)
    {
        var tempOutput = Path.GetTempFileName();

        var processInfo = new ProcessStartInfo
        {
            FileName = "cmd.exe",
            Arguments = $"/c {fileName} {arguments} > \"{tempOutput}\" 2>&1",
            UseShellExecute = true,
            Verb = "runas",
            WindowStyle = ProcessWindowStyle.Hidden
        };

        try
        {
            using var process = Process.Start(processInfo);
            process?.WaitForExit();

            if (process == null || process.ExitCode == 0) return;

            var output = File.Exists(tempOutput)
                ? File.ReadAllText(tempOutput).Trim()
                : string.Empty;

            throw new ElevatedCommandException("Elevated command failed.", $"{fileName} {arguments}", output);
        }
        catch (System.ComponentModel.Win32Exception ex) when (ex.NativeErrorCode == 1223)
        {
            // User cancelled the UAC prompt
            throw new OperationCanceledException("User cancelled the UAC prompt.", ex);
        }
        finally
        {
            if (File.Exists(tempOutput))
                try { File.Delete(tempOutput); } catch { /* ignored */ }
        }
    }

    public string GetLogs(string taskPath)
    {
        try
        {
            var logs = new List<string>();
            string query = $"*[System[Provider[@Name='Microsoft-Windows-TaskScheduler']]] and *[EventData[Data[@Name='TaskName']='{taskPath}']]";
            var elq = new System.Diagnostics.Eventing.Reader.EventLogQuery("Microsoft-Windows-TaskScheduler/Operational", System.Diagnostics.Eventing.Reader.PathType.LogName, query) { ReverseDirection = true };
            using var reader = new System.Diagnostics.Eventing.Reader.EventLogReader(elq);
            System.Diagnostics.Eventing.Reader.EventRecord record;
            int count = 0;
            while ((record = reader.ReadEvent()) != null && count < 200)
            {
                try 
                {
                    string level = record.LevelDisplayName == "Information" ? "Info" :
                                   record.LevelDisplayName == "Warning" ? "Warn" :
                                   record.LevelDisplayName;
                    logs.Add($"[{record.TimeCreated:yyyy-MM-dd HH:mm:ss}] [{level}] {record.FormatDescription()}");
                }
                catch 
                {
                    string level = record.LevelDisplayName == "Information" ? "Info" :
                                   record.LevelDisplayName == "Warning" ? "Warn" :
                                   record.LevelDisplayName;
                    logs.Add($"[{record.TimeCreated:yyyy-MM-dd HH:mm:ss}] [{level}] (Log description unavailable)");
                }
                count++;
            }
            if (logs.Count == 0) 
            {
                try
                {
                    var config = new System.Diagnostics.Eventing.Reader.EventLogConfiguration("Microsoft-Windows-TaskScheduler/Operational");
                    if (!config.IsEnabled)
                    {
                        return "The Task Scheduler Operational event log is disabled.\nTask history is not being recorded.\n\nTo enable it, open Task Scheduler and click 'Enable All Tasks History' in the Actions pane, or enable the 'Microsoft-Windows-TaskScheduler/Operational' log via Event Viewer.";
                    }
                }
                catch { } // Ignore if we can't read configuration

                return "No logs found for this task.";
            }
            
            return string.Join("\n", logs);
        }
        catch (Exception ex)
        {
            return $"Failed to get logs: {ex.Message}";
        }
    }
}
