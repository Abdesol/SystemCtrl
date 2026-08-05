using System;
using Microsoft.Win32.TaskScheduler;
using ReactiveUI;
using ReactiveUI.SourceGenerators;
using SystemCtrl.Core.Exceptions;
using SystemCtrl.Core.Interfaces;
using SystemCtrl.Core.Models;
using SystemCtrl.Desktop.Services;

namespace SystemCtrl.Desktop.ViewModels;

public partial class TaskDetailViewModel : ViewModelBase
{
    private readonly IWindowsTaskManager _windowsTaskManager;
    private readonly IAiAnalyzer _aiAnalyzer;
    private readonly ISettingsService _settingsService;
    private readonly IErrorDialogService _errorDialog;


    public TaskDetailViewModel(
        IWindowsTaskManager windowsTaskManager,
        IAiAnalyzer aiAnalyzer,
        ISettingsService settingsService,
        IErrorDialogService errorDialog)
    {
        _windowsTaskManager = windowsTaskManager;
        _aiAnalyzer = aiAnalyzer;
        _settingsService = settingsService;
        _errorDialog = errorDialog;



        this.WhenAnyValue(
                x => x.IsEnabled,
                x => x.IsExecutingAction,
                (enabled, executing) => enabled && !executing)
            .Subscribe(can => CanExecuteTaskActions = can);

        this.WhenAnyValue(x => x.IsAiSummaryExpanded)
            .Subscribe(expanded => ToggleAiSummaryText = expanded ? "Hide" : "Show");
    }

    [Reactive] public partial WindowsTaskInfo? Task { get; set; }

    [Reactive] public partial DetailedWindowsTaskInfo? DetailedInfo { get; set; }

    [Reactive] public partial string? Logs { get; set; }

    [Reactive] public partial bool IsLoadingDetails { get; set; }

    [Reactive] public partial bool IsEnabled { get; set; }

    [Reactive] public partial bool IsExecutingAction { get; set; }

    [Reactive] public partial bool CanExecuteTaskActions { get; set; }

    [Reactive] public partial System.Collections.Generic.List<AiQna>? AiSummaryList { get; set; }

    [Reactive] public partial bool IsGeneratingSummary { get; set; }

    [Reactive] public partial bool HasAiSummary { get; set; }

    [Reactive] public partial bool IsAiSummaryExpanded { get; set; }

    [Reactive] public partial bool ShowAiSummary { get; set; }

    [Reactive] public partial string ToggleAiSummaryText { get; set; } = "Show";

    [Reactive] public partial string LoadingSummaryText { get; set; } = "Generating insights...";

    [Reactive] public partial bool HasApiKey { get; set; }

    [Reactive] public partial bool ShowLogsTab { get; set; } = true;

    public IObservable<bool> CanRunTask =>
        this.WhenAnyValue(x => x.Task, x => x.Task!.Status,
            (t, status) => t != null && status != TaskState.Running);

    public IObservable<bool> CanStopTask =>
        this.WhenAnyValue(x => x.Task, x => x.Task!.Status,
            (t, status) => t != null && status == TaskState.Running);

    public void Load(WindowsTaskInfo task)
    {
        Task = task;
        DetailedInfo = null;
        IsLoadingDetails = true;
        IsEnabled = task.Status != TaskState.Disabled;

        AiSummaryList = null;
        HasAiSummary = false;
        IsGeneratingSummary = false;
        IsAiSummaryExpanded = false;

        if (task != null!)
        {
            var settings = _settingsService.LoadSettings();
            ShowAiSummary = settings.ShowAiSummary;
            ShowLogsTab = !settings.DisableScheduledTasksLogs;
            HasApiKey = !string.IsNullOrWhiteSpace(settings.GeminiApiKey);
            if (settings.AiSummaries.TryGetValue(task.TaskName, out var existingSummary))
            {
                AiSummaryList = existingSummary;
                HasAiSummary = true;
            }

            _ = LoadDetailsAsync(task);
        }
    }


    private async System.Threading.Tasks.Task LoadDetailsAsync(WindowsTaskInfo task)
    {
        IsLoadingDetails = true;
        
        var detailedInfoTask = System.Threading.Tasks.Task.Run(() =>
        {
            try
            {
                return _windowsTaskManager.GetDetailedInfo(task.TaskPath);
            }
            catch
            {
                return null;
            }
        });

        var logsTask = System.Threading.Tasks.Task.Run(() =>
        {
            if (!ShowLogsTab) return null;
            try
            {
                return _windowsTaskManager.GetLogs(task.TaskPath);
            }
            catch
            {
                return "Failed to fetch logs.";
            }
        });

        await System.Threading.Tasks.Task.WhenAll(detailedInfoTask, logsTask);
        DetailedInfo = detailedInfoTask.Result;
        Logs = logsTask.Result;
        
        IsLoadingDetails = false;
    }

    [ReactiveCommand(CanExecute = nameof(CanRunTask))]
    public async System.Threading.Tasks.Task RunTask()
    {
        if (Task == null) return;
        IsExecutingAction = true;
        try
        {
            await System.Threading.Tasks.Task.Run(() => _windowsTaskManager.Run(Task.TaskPath));
            await System.Threading.Tasks.Task.Delay(800);
            RefreshTaskState();
        }
        catch (OperationCanceledException)
        {
            // User cancelled the action, do nothing
        }
        catch (ElevatedCommandException ex)
        {
            await _errorDialog.ShowAsync($"Failed to run '{Task.TaskName}'", ex.Message, ex.ToString());
        }
        catch (Exception ex)
        {
            await _errorDialog.ShowAsync($"Failed to run '{Task.TaskName}'", ex.Message, ex.ToString());
        }
        finally
        {
            IsExecutingAction = false;
        }
    }

    [ReactiveCommand(CanExecute = nameof(CanStopTask))]
    public async System.Threading.Tasks.Task StopTask()
    {
        if (Task == null) return;
        IsExecutingAction = true;
        try
        {
            await System.Threading.Tasks.Task.Run(() => _windowsTaskManager.Stop(Task.TaskPath));
            await System.Threading.Tasks.Task.Delay(800);
            RefreshTaskState();
        }
        catch (OperationCanceledException)
        {
            // User cancelled the action, do nothing
        }
        catch (ElevatedCommandException ex)
        {
            await _errorDialog.ShowAsync($"Failed to stop '{Task.TaskName}'", ex.Message, ex.ToString());
        }
        catch (Exception ex)
        {
            await _errorDialog.ShowAsync($"Failed to stop '{Task.TaskName}'", ex.Message, ex.ToString());
        }
        finally
        {
            IsExecutingAction = false;
        }
    }

    [ReactiveCommand]
    public async System.Threading.Tasks.Task EnableTask()
    {
        if (Task == null) return;
        IsExecutingAction = true;
        try
        {
            await System.Threading.Tasks.Task.Run(() => _windowsTaskManager.Enable(Task.TaskPath));
            IsEnabled = true;
            Task.Status = TaskState.Ready;
        }
        catch (OperationCanceledException)
        {
            // User cancelled the action, do nothing
        }
        catch (ElevatedCommandException ex)
        {
            await _errorDialog.ShowAsync($"Failed to enable '{Task.TaskName}'", ex.Message, ex.ToString());
        }
        catch (Exception ex)
        {
            await _errorDialog.ShowAsync($"Failed to enable '{Task.TaskName}'", ex.Message, ex.ToString());
        }
        finally
        {
            IsExecutingAction = false;
        }
    }

    [ReactiveCommand]
    public async System.Threading.Tasks.Task DisableTask()
    {
        if (Task == null) return;
        IsExecutingAction = true;
        try
        {
            await System.Threading.Tasks.Task.Run(() => _windowsTaskManager.Disable(Task.TaskPath));
            IsEnabled = false;
            Task.Status = TaskState.Disabled;
        }
        catch (OperationCanceledException)
        {
            // User cancelled the action, do nothing
        }
        catch (ElevatedCommandException ex)
        {
            await _errorDialog.ShowAsync($"Failed to disable '{Task.TaskName}'", ex.Message, ex.ToString());
        }
        catch (Exception ex)
        {
            await _errorDialog.ShowAsync($"Failed to disable '{Task.TaskName}'", ex.Message, ex.ToString());
        }
        finally
        {
            IsExecutingAction = false;
        }
    }

    private void RefreshTaskState()
    {
        if (Task == null) return;
        try
        {
            var updated = System.Linq.Enumerable.FirstOrDefault(
                _windowsTaskManager.GetTasks(true),
                t => t.TaskPath == Task.TaskPath);

            if (updated != null)
            {
                Task.Status = updated.Status;
                Task.NextRunTime = updated.NextRunTime;
                Task.LastRunTime = updated.LastRunTime;
                Task.LastRunResult = updated.LastRunResult;
                IsEnabled = Task.Status != TaskState.Disabled;
            }
        }
        catch
        {
            /* ignored */
        }
    }

    [ReactiveCommand]
    public void ToggleAiSummary()
    {
        IsAiSummaryExpanded = !IsAiSummaryExpanded;
    }

    [ReactiveCommand]
    public async System.Threading.Tasks.Task GenerateAiSummary()
    {
        if (DetailedInfo == null || Task == null) return;

        var previousSummary = AiSummaryList;
        var hadPreviousSummary = HasAiSummary;
        var wasExpanded = IsAiSummaryExpanded;

        IsGeneratingSummary = true;
        HasAiSummary = false;
        IsAiSummaryExpanded = false;

        string[] loadingTexts =
        [
            "Analyzing scheduled task...",
            "Connecting to Gemini...",
            "Synthesizing insights...",
            "Generating insights..."
        ];

        _ = System.Threading.Tasks.Task.Run(async () =>
        {
            var i = 0;
            while (IsGeneratingSummary)
            {
                LoadingSummaryText = loadingTexts[i % loadingTexts.Length];
                i++;
                await System.Threading.Tasks.Task.Delay(TimeSpan.FromSeconds(2));
            }
        });

        var settings = _settingsService.LoadSettings();
        try
        {
            var summaryList = await _aiAnalyzer.AnalyzeTaskAsync(Task, DetailedInfo, settings);

            AiSummaryList = summaryList;
            HasAiSummary = true;
            IsAiSummaryExpanded = true;

            settings.AiSummaries[Task.TaskName] = summaryList;
            _settingsService.SaveSettings(settings);
        }
        catch (Exception ex)
        {
            await _errorDialog.ShowAsync("AI Analysis Failed", "", ex.Message);
            AiSummaryList = previousSummary;
            HasAiSummary = hadPreviousSummary;
            IsAiSummaryExpanded = wasExpanded;
        }
        finally
        {
            IsGeneratingSummary = false;
        }
    }
}