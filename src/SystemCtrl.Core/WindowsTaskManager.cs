using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;
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
        var csvData = RunSchTasks("/query /v /fo CSV");
        if (string.IsNullOrWhiteSpace(csvData))
        {
            return Enumerable.Empty<WindowsTaskInfo>();
        }

        return ParseCsv(csvData, includeSystemTasks);
    }

    private static string RunSchTasks(string arguments)
    {
        var psi = new ProcessStartInfo
        {
            FileName = "schtasks.exe",
            Arguments = arguments,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            CreateNoWindow = true,
            StandardOutputEncoding = Encoding.UTF8
        };
        try
        {
            using var proc = Process.Start(psi);
            if (proc == null) return string.Empty;
            string output = proc.StandardOutput.ReadToEnd();
            proc.WaitForExit();
            return output;
        }
        catch
        {
            return string.Empty;
        }
    }

    private IEnumerable<WindowsTaskInfo> ParseCsv(string csvData, bool includeSystemTasks)
    {
        var lines = csvData.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        if (lines.Length <= 1) yield break;

        var headers = Helpers.CsvHelper.ParseCsvLine(lines[0]);
        var colMap = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        for (int i = 0; i < headers.Length; i++)
        {
            colMap[headers[i]] = i;
        }

        for (int i = 1; i < lines.Length; i++)
        {
            var cols = Helpers.CsvHelper.ParseCsvLine(lines[i]);
            if (cols.Length != headers.Length) continue;

            string GetCol(string name) => colMap.TryGetValue(name, out var idx) ? cols[idx] : string.Empty;

            var taskPath = GetCol("TaskName");
            if (string.IsNullOrWhiteSpace(taskPath)) continue;

            if (!includeSystemTasks && IsSystemTask(taskPath))
                continue;

            var taskName = Path.GetFileName(taskPath.TrimEnd('\\'));
            if (string.IsNullOrEmpty(taskName)) taskName = taskPath;

            DateTime? nextRun = null;
            if (DateTime.TryParse(GetCol("Next Run Time"), out var nr)) nextRun = nr;

            DateTime? lastRun = null;
            if (DateTime.TryParse(GetCol("Last Run Time"), out var lr)) lastRun = lr;

            int.TryParse(GetCol("Last Result"), out var lrResult);

            var statusStr = GetCol("Status");
            var schedState = GetCol("Scheduled Task State");
            
            var status = TaskState.Unknown;
            if (schedState.Contains("Disabled", StringComparison.OrdinalIgnoreCase)) status = TaskState.Disabled;
            else if (statusStr.Contains("Running", StringComparison.OrdinalIgnoreCase)) status = TaskState.Running;
            else if (statusStr.Contains("Ready", StringComparison.OrdinalIgnoreCase)) status = TaskState.Ready;
            else if (statusStr.Contains("Disabled", StringComparison.OrdinalIgnoreCase)) status = TaskState.Disabled;

            yield return new WindowsTaskInfo
            {
                TaskName = taskName,
                TaskPath = taskPath,
                Description = GetCol("Comment"),
                Author = GetCol("Author"),
                Status = status,
                TriggerSummary = GetCol("Schedule"),
                NextRunTime = nextRun,
                LastRunTime = lastRun,
                LastRunResult = lrResult,
                RunAsUser = GetCol("Run As User")
            };
        }
    }

    public DetailedWindowsTaskInfo GetDetailedInfo(string taskPath)
    {
        var csvData = RunSchTasks($"/query /v /fo CSV /tn \"{taskPath}\"");
        if (string.IsNullOrWhiteSpace(csvData))
        {
            return new DetailedWindowsTaskInfo { TaskPath = taskPath };
        }
        
        var lines = csvData.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        if (lines.Length <= 1) return new DetailedWindowsTaskInfo { TaskPath = taskPath };
        
        var headers = Helpers.CsvHelper.ParseCsvLine(lines[0]);
        var cols = Helpers.CsvHelper.ParseCsvLine(lines[1]);
        if (cols.Length != headers.Length) return new DetailedWindowsTaskInfo { TaskPath = taskPath };
        
        var colMap = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        for (int i = 0; i < headers.Length; i++) colMap[headers[i]] = i;
        string GetCol(string name) => colMap.TryGetValue(name, out var idx) ? cols[idx] : string.Empty;
        
        var taskName = Path.GetFileName(taskPath.TrimEnd('\\'));
        if (string.IsNullOrEmpty(taskName)) taskName = taskPath;

        var scheduleType = GetCol("Schedule Type");
        var schedule = GetCol("Schedule");
        
        var triggers = new List<string>();
        if (!string.IsNullOrWhiteSpace(schedule))
        {
            triggers.Add(schedule);
        }
        
        var actions = new List<string>();
        var taskToRun = GetCol("Task To Run");
        if (!string.IsNullOrWhiteSpace(taskToRun))
        {
            actions.Add(taskToRun);
        }

        return new DetailedWindowsTaskInfo
        {
            TaskName = taskName,
            TaskPath = taskPath,
            Actions = actions.ToArray(),
            Triggers = triggers.ToArray(),
            RunAsUser = GetCol("Run As User"),
            CompatibilityLevel = "Unknown",
            IsHidden = false,
            ExecutionTimeLimit = GetCol("Stop Task If Runs X Hours and X Mins"),
            MultipleInstancesPolicy = "Unknown"
        };
    }

    public void Enable(string taskPath)
    {
        ExecuteElevated("schtasks.exe", $"/Change /TN \"{taskPath}\" /Enable");
    }

    public void Disable(string taskPath)
    {
        ExecuteElevated("schtasks.exe", $"/Change /TN \"{taskPath}\" /Disable");
    }

    public void Run(string taskPath)
    {
        ExecuteElevated("schtasks.exe", $"/Run /TN \"{taskPath}\"");
    }

    public void Stop(string taskPath)
    {
        ExecuteElevated("schtasks.exe", $"/End /TN \"{taskPath}\"");
    }

    private static bool IsSystemTask(string taskPath)
    {
        return SystemTaskFolders.Any(f =>
            taskPath.StartsWith(f, StringComparison.OrdinalIgnoreCase));
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
}
