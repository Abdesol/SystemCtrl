using System;
using System.ServiceProcess;
using System.Threading.Tasks;
using SystemCtrl.Core.Interfaces;
using SystemCtrl.Core.Models;
using ReactiveUI;
using ReactiveUI.SourceGenerators;
using System.Reactive.Linq;
using ReactiveUI.Avalonia;
using SystemCtrl.Core.Exceptions;
using SystemCtrl.Desktop.Services;

namespace SystemCtrl.Desktop.ViewModels;

public partial class ServiceDetailViewModel : ViewModelBase
{
    private readonly IWindowsServiceManager _windowsServiceManager;
    private readonly IAiAnalyzer _serviceAnalyzer;
    private readonly ISettingsService _settingsService;
    private readonly IErrorDialogService _errorDialog;

    private readonly IElevatedResourceMonitorService _resourceMonitor;
    private IDisposable? _resourceMonitorSubscription;

    public ServiceDetailViewModel(
        IWindowsServiceManager windowsServiceManager,
        IAiAnalyzer serviceAnalyzer,
        ISettingsService settingsService,
        IErrorDialogService errorDialog,
        IElevatedResourceMonitorService resourceMonitor)
    {
        _windowsServiceManager = windowsServiceManager;
        _serviceAnalyzer = serviceAnalyzer;
        _settingsService = settingsService;
        _errorDialog = errorDialog;
        _resourceMonitor = resourceMonitor;

        this.WhenAnyValue(x => x.IsAiSummaryExpanded)
            .Subscribe(expanded => ToggleAiSummaryText = expanded ? "Hide" : "Show");

        this.WhenAnyValue(x => x.IsResourceUsageExpanded, x => x.DetailedInfo)
            .Subscribe(t => HandleResourceUsageExpansion(t.Item1));

        this.WhenAnyValue(
                x => x.IsDisabled,
                x => x.IsExecutingAction,
                (isDisabled, isExecuting) => !isDisabled && !isExecuting)
            .Subscribe(canExecute => CanExecuteServiceActions = canExecute);

        this.WhenAnyValue(
                x => x.IsDisabled,
                x => x.IsExecutingAction,
                (isDisabled, isExecuting) => isDisabled && !isExecuting)
            .Subscribe(canEnable => CanEnableService = canEnable);

        this.WhenAnyValue(
                x => x.IsDisabled,
                x => x.IsExecutingAction,
                (isDisabled, isExecuting) => !isDisabled && !isExecuting)
            .Subscribe(canDisable => CanDisableService = canDisable);
            
        this.WhenAnyValue(x => x.SelectedTabIndex, x => x.Service, x => x.ShowLogsTab)
            .Subscribe(t => 
            {
                var tabIndex = t.Item1;
                var service = t.Item2;
                var showLogs = t.Item3;
                
                _logStreamSubscription?.Dispose();
                _logStreamSubscription = null;
                
                if (tabIndex == 1 && service != null && showLogs)
                {
                    _logStreamSubscription = _windowsServiceManager.StreamLogs(service.ServiceName)
                        .ObserveOn(AvaloniaScheduler.Instance)
                        .Subscribe(logLine => 
                        {
                            LogsList.Insert(0, logLine);
                        });
                }
            });
    }
    
    [Reactive] public partial bool CanExecuteServiceActions { get; set; }
    
    [Reactive] public partial bool CanEnableService { get; set; }
    
    [Reactive] public partial bool CanDisableService { get; set; }
    [Reactive] public partial WindowsServiceInfo? Service { get; set; }

    [Reactive] public partial DetailedWindowsServiceInfo? DetailedInfo { get; set; }

    [Reactive] public partial System.Collections.ObjectModel.ObservableCollection<string> LogsList { get; set; } = new();
    
    [Reactive] public partial int SelectedTabIndex { get; set; }
    private IDisposable? _logStreamSubscription;

    [Reactive] public partial bool IsLoadingDetails { get; set; }

    [Reactive] public partial bool IsAutoStart { get; set; }

    [Reactive] public partial bool IsDisabled { get; set; }

    [Reactive] public partial System.Collections.Generic.List<AiQna>? AiSummaryList { get; set; }

    [Reactive] public partial bool IsGeneratingSummary { get; set; }

    [Reactive] public partial bool HasAiSummary { get; set; }

    [Reactive] public partial bool IsAiSummaryExpanded { get; set; }

    [Reactive] public partial bool ShowAiSummary { get; set; }
    
    [Reactive] public partial bool ShowResourceUsage { get; set; }

    [Reactive] public partial string ToggleAiSummaryText { get; set; } = "Show";

    [Reactive] public partial bool IsResourceUsageExpanded { get; set; }
    
    [Reactive] public partial string ToggleResourceUsageText { get; set; } = "Show";

    [Reactive] public partial string? LiveCpuPercent { get; set; }
    
    [Reactive] public partial string? LiveMemoryFormatted { get; set; }
    
    [Reactive] public partial string? LiveRunningForFormatted { get; set; }

    [Reactive] public partial string LoadingSummaryText { get; set; } = "Generating insights...";

    [Reactive] public partial bool HasApiKey { get; set; }

    [Reactive] public partial bool ShowLogsTab { get; set; } = true;

    public void Load(WindowsServiceInfo service)
    {
        Service = service;
        DetailedInfo = null;
        IsLoadingDetails = true;

        AiSummaryList = null;
        HasAiSummary = false;
        IsGeneratingSummary = false;
        IsAiSummaryExpanded = false;
        IsResourceUsageExpanded = false;

        if (service != null!)
        {
            IsAutoStart = service.StartType == ServiceStartMode.Automatic ||
                          service.StartType == ServiceStartMode.Boot ||
                          service.StartType == ServiceStartMode.System;

            IsDisabled = service.StartType == ServiceStartMode.Disabled;

            var settings = _settingsService.LoadSettings();
            ShowAiSummary = settings.ShowAiSummary;
            ShowResourceUsage = !settings.DisableResourceUsage;
            ShowLogsTab = !settings.DisableServiceLogs;
            HasApiKey = !string.IsNullOrWhiteSpace(settings.GeminiApiKey);
            if (settings.AiSummaries.TryGetValue(service.ServiceName, out var existingSummary))
            {
                AiSummaryList = existingSummary;
                HasAiSummary = true;
            }

            _ = LoadDetailsAsync(service);
        }
    }

    private void HandleResourceUsageExpansion(bool expanded)
    {
        ToggleResourceUsageText = expanded ? "Hide" : "Show";

        _resourceMonitorSubscription?.Dispose();
        _resourceMonitorSubscription = null;

        if (expanded && DetailedInfo?.ProcessId is { } pid)
        {
            LiveCpuPercent = "Loading...";
            LiveMemoryFormatted = "Loading...";
            LiveRunningForFormatted = "Loading...";

            _resourceMonitorSubscription = _resourceMonitor.MonitorProcess(pid)
                .ObserveOn(AvaloniaScheduler.Instance)
                .Subscribe(update =>
                {
                    if (update.Error != null)
                    {
                        LiveCpuPercent = "Error";
                        LiveMemoryFormatted = "Error";
                        LiveRunningForFormatted = "Error";
                    }
                    else
                    {
                        LiveCpuPercent = update.CpuPercent?.ToString("0.0") + " %";
                        LiveMemoryFormatted = update.MemoryBytes.HasValue ? (update.MemoryBytes.Value / 1024 / 1024).ToString("N0") + " MB" : "-";
                        
                        if (update.RunningFor.HasValue)
                        {
                            var runningFor = update.RunningFor.Value;
                            if (runningFor.TotalDays >= 1)
                            {
                                LiveRunningForFormatted = $@"{runningFor.Days}d {runningFor:hh\:mm\:ss}";
                            }
                            else
                            {
                                LiveRunningForFormatted = $@"{runningFor:hh\:mm\:ss}";
                            }
                        }
                        else
                        {
                            LiveRunningForFormatted = "-";
                        }
                    }
                });
        }
        else if (expanded && DetailedInfo?.ProcessId == null)
        {
             LiveCpuPercent = "-";
             LiveMemoryFormatted = "-";
             LiveRunningForFormatted = "-";
        }
        else
        {
            _resourceMonitor.StopMonitoring();
        }
    }

    [ReactiveCommand]
    public void ToggleResourceUsage()
    {
        IsResourceUsageExpanded = !IsResourceUsageExpanded;
    }

    private async Task LoadDetailsAsync(WindowsServiceInfo service)
    {
        IsLoadingDetails = true;
        
        var detailedInfoTask = Task.Run(() =>
        {
            try
            {
                return _windowsServiceManager.GetDetailedInfo(service!.ServiceName);
            }
            catch
            {
                return null;
            }
        });

        var logsTask = Task.Run(() =>
        {
            if (!ShowLogsTab) return null;
            try
            {
                return _windowsServiceManager.GetLogs(service!.ServiceName);
            }
            catch
            {
                return new System.Collections.Generic.List<string> { "Failed to fetch logs." };
            }
        });

        await Task.WhenAll(detailedInfoTask, logsTask);
        DetailedInfo = detailedInfoTask.Result;
        
        if (logsTask.Result != null)
        {
            LogsList = new System.Collections.ObjectModel.ObservableCollection<string>(logsTask.Result);
        }
        
        IsLoadingDetails = false;
    }

    [Reactive] public partial bool IsExecutingAction { get; set; }

    public IObservable<bool> CanStartService =>
        this.WhenAnyValue(x => x.Service, x => x.Service!.Status,
            (svc, status) => svc != null && status == ServiceControllerStatus.Stopped);

    public IObservable<bool> CanStopService =>
        this.WhenAnyValue(x => x.Service, x => x.Service!.Status,
            (svc, status) => svc != null && (status == ServiceControllerStatus.Running ||
                                             status == ServiceControllerStatus.Paused));

    [ReactiveCommand(CanExecute = nameof(CanStartService))]
    public async Task StartService()
    {
        if (Service == null) return;
        IsExecutingAction = true;
        try
        {
            await Task.Run(() => _windowsServiceManager.Start(Service.ServiceName));
            await Task.Delay(1000);
            RefreshServiceState();
        }
        catch (OperationCanceledException)
        {
            // User cancelled the action, do nothing
        }
        catch (ElevatedCommandException ex)
        {
            await _errorDialog.ShowAsync(
                $"Failed to start '{Service.DisplayName}'",
                ex.Message,
                ex.ToString());
        }
        catch (Exception ex)
        {
            await _errorDialog.ShowAsync(
                $"Failed to start '{Service.DisplayName}'",
                ex.Message,
                ex.ToString());
        }
        finally
        {
            IsExecutingAction = false;
        }
    }

    [ReactiveCommand(CanExecute = nameof(CanStopService))]
    public async Task StopService()
    {
        if (Service == null) return;
        IsExecutingAction = true;
        try
        {
            await Task.Run(() => _windowsServiceManager.Stop(Service.ServiceName));
            await Task.Delay(1000);
            RefreshServiceState();
            IsResourceUsageExpanded = false;
        }
        catch (OperationCanceledException)
        {
            // User cancelled the action, do nothing
        }
        catch (ElevatedCommandException ex)
        {
            await _errorDialog.ShowAsync(
                $"Failed to stop '{Service.DisplayName}'",
                ex.Message,
                ex.ToString());
        }
        catch (Exception ex)
        {
            await _errorDialog.ShowAsync(
                $"Failed to stop '{Service.DisplayName}'",
                ex.Message,
                ex.ToString());
        }
        finally
        {
            IsExecutingAction = false;
        }
    }

    public IObservable<bool> CanRestartService => CanStopService;

    [ReactiveCommand(CanExecute = nameof(CanRestartService))]
    public async Task RestartService()
    {
        if (Service == null) return;
        IsExecutingAction = true;
        try
        {
            await Task.Run(() => _windowsServiceManager.Restart(Service.ServiceName));
            await Task.Delay(1000);
            RefreshServiceState();
        }
        catch (OperationCanceledException)
        {
            // User cancelled the action, do nothing
        }
        catch (ElevatedCommandException ex)
        {
            await _errorDialog.ShowAsync(
                $"Failed to restart '{Service.DisplayName}'",
                ex.Message,
                ex.ToString());
        }
        catch (Exception ex)
        {
            await _errorDialog.ShowAsync(
                $"Failed to restart '{Service.DisplayName}'",
                ex.Message,
                ex.ToString());
        }
        finally
        {
            IsExecutingAction = false;
        }
    }

    [ReactiveCommand]
    public async Task DisableAutoStart()
    {
        if (Service == null) return;
        IsExecutingAction = true;
        try
        {
            await Task.Run(() =>
                _windowsServiceManager.SetStartType(Service.ServiceName, ServiceStartMode.Manual));
            IsAutoStart = false;
            Service.StartType = ServiceStartMode.Manual;
        }
        catch (OperationCanceledException)
        {
            // User cancelled the action, do nothing
        }
        catch (ElevatedCommandException ex)
        {
            await _errorDialog.ShowAsync(
                $"Failed to disable auto-start for '{Service.DisplayName}'",
                ex.Message,
                ex.ToString());
        }
        catch (Exception ex)
        {
            await _errorDialog.ShowAsync(
                $"Failed to disable auto-start for '{Service.DisplayName}'",
                ex.Message,
                ex.ToString());
        }
        finally
        {
            IsExecutingAction = false;
        }
    }

    [ReactiveCommand]
    public async Task EnableAutoStart()
    {
        if (Service == null) return;
        IsExecutingAction = true;
        try
        {
            await Task.Run(() =>
                _windowsServiceManager.SetStartType(Service.ServiceName,
                    ServiceStartMode.Automatic));
            IsAutoStart = true;
            Service.StartType = ServiceStartMode.Automatic;
        }
        catch (OperationCanceledException)
        {
            // User cancelled the action, do nothing
        }
        catch (ElevatedCommandException ex)
        {
            await _errorDialog.ShowAsync(
                $"Failed to enable auto-start for '{Service.DisplayName}'",
                ex.Message,
                ex.ToString());
        }
        catch (Exception ex)
        {
            await _errorDialog.ShowAsync(
                $"Failed to enable auto-start for '{Service.DisplayName}'",
                ex.Message,
                ex.ToString());
        }
        finally
        {
            IsExecutingAction = false;
        }
    }

    [ReactiveCommand]
    public async Task EnableService()
    {
        if (Service == null) return;
        IsExecutingAction = true;
        try
        {
            await Task.Run(() =>
                _windowsServiceManager.SetStartType(Service.ServiceName, ServiceStartMode.Manual));
            IsDisabled = false;
            Service.StartType = ServiceStartMode.Manual;
            RefreshServiceState();
        }
        catch (OperationCanceledException)
        {
            // User cancelled the action, do nothing
        }
        catch (ElevatedCommandException ex)
        {
            await _errorDialog.ShowAsync(
                $"Failed to enable service '{Service.DisplayName}'",
                ex.Message,
                ex.ToString());
        }
        catch (Exception ex)
        {
            await _errorDialog.ShowAsync(
                $"Failed to enable service '{Service.DisplayName}'",
                ex.Message,
                ex.ToString());
        }
        finally
        {
            IsExecutingAction = false;
        }
    }

    [ReactiveCommand]
    public async Task DisableService()
    {
        if (Service == null) return;
        IsExecutingAction = true;
        try
        {
            await Task.Run(() =>
                _windowsServiceManager.SetStartType(Service.ServiceName, ServiceStartMode.Disabled));
            IsDisabled = true;
            Service.StartType = ServiceStartMode.Disabled;
            RefreshServiceState();
        }
        catch (OperationCanceledException)
        {
            // User cancelled the action, do nothing
        }
        catch (ElevatedCommandException ex)
        {
            await _errorDialog.ShowAsync(
                $"Failed to disable service '{Service.DisplayName}'",
                ex.Message,
                ex.ToString());
        }
        catch (Exception ex)
        {
            await _errorDialog.ShowAsync(
                $"Failed to disable service '{Service.DisplayName}'",
                ex.Message,
                ex.ToString());
        }
        finally
        {
            IsExecutingAction = false;
        }
    }

    private void RefreshServiceState()
    {
        if (Service == null) return;
        try
        {
            var updated = System.Linq.Enumerable.FirstOrDefault(_windowsServiceManager.GetServices(true),
                s => s.ServiceName == Service.ServiceName);
            
            if (updated != null)
            {
                Service.Status = updated.Status;
                Service.StartType = updated.StartType;

                IsAutoStart = Service.StartType == ServiceStartMode.Automatic ||
                              Service.StartType == ServiceStartMode.Boot ||
                              Service.StartType == ServiceStartMode.System;
                IsDisabled = Service.StartType == ServiceStartMode.Disabled;
                
                _ = LoadDetailsAsync(Service);
            }
        }
        catch
        {
            // ignored
        }
    }

    [ReactiveCommand]
    public void ToggleAiSummary()
    {
        IsAiSummaryExpanded = !IsAiSummaryExpanded;
    }

    [ReactiveCommand]
    public async Task GenerateAiSummary()
    {
        if (DetailedInfo == null || Service == null) return;

        var previousSummary = AiSummaryList;
        var hadPreviousSummary = HasAiSummary;
        var wasExpanded = IsAiSummaryExpanded;

        IsGeneratingSummary = true;
        HasAiSummary = false;
        IsAiSummaryExpanded = false;

        string[] loadingTexts =
        [
            "Analyzing service behavior...",
            "Connecting to Gemini...",
            "Synthesizing insights...",
            "Generating insights..."
        ];
        _ = Task.Run(async () =>
        {
            var i = 0;
            while (IsGeneratingSummary)
            {
                LoadingSummaryText = loadingTexts[i % loadingTexts.Length];
                i++;
                await Task.Delay(TimeSpan.FromSeconds(2));
            }
        });

        var settings = _settingsService.LoadSettings();
        try
        {
            var summaryList = await _serviceAnalyzer.AnalyzeServiceAsync(DetailedInfo, settings);

            AiSummaryList = summaryList;
            HasAiSummary = true;
            IsAiSummaryExpanded = true;

            settings.AiSummaries[Service.ServiceName] = summaryList;
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
