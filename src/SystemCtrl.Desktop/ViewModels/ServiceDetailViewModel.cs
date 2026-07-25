using System;
using System.Threading.Tasks;
using SystemCtrl.Core.Interfaces;
using SystemCtrl.Core.Models;
using ReactiveUI;
using ReactiveUI.SourceGenerators;

namespace SystemCtrl.Desktop.ViewModels;

public partial class ServiceDetailViewModel : ViewModelBase
{
    private readonly IWindowsServiceManager _windowsServiceManager;
    private readonly IServiceAnalyzer _serviceAnalyzer;
    private readonly ISettingsService _settingsService;

    public ServiceDetailViewModel(
        IWindowsServiceManager windowsServiceManager,
        IServiceAnalyzer serviceAnalyzer,
        ISettingsService settingsService)
    {
        _windowsServiceManager = windowsServiceManager;
        _serviceAnalyzer = serviceAnalyzer;
        _settingsService = settingsService;
    }

    [Reactive] public partial WindowsServiceInfo? Service { get; set; }

    [Reactive] public partial DetailedWindowsServiceInfo? DetailedInfo { get; set; }

    [Reactive] public partial bool IsLoadingDetails { get; set; }

    [Reactive] public partial bool IsAutoStart { get; set; }

    [Reactive] public partial System.Collections.Generic.List<AiQna>? AiSummaryList { get; set; }

    [Reactive] public partial bool IsGeneratingSummary { get; set; }

    [Reactive] public partial bool HasAiSummary { get; set; }

    [Reactive] public partial bool IsAiSummaryExpanded { get; set; }

    public string ToggleAiSummaryText => IsAiSummaryExpanded ? "Hide" : "Show";

    [Reactive] public partial string LoadingSummaryText { get; set; } = "Generating insights...";

    public void Load(WindowsServiceInfo service)
    {
        Service = service;
        DetailedInfo = null;
        IsLoadingDetails = true;

        AiSummaryList = null;
        HasAiSummary = false;
        IsGeneratingSummary = false;
        IsAiSummaryExpanded = false;
        this.RaisePropertyChanged(nameof(ToggleAiSummaryText));

        if (service != null!)
        {
            IsAutoStart = service.StartType == System.ServiceProcess.ServiceStartMode.Automatic ||
                          service.StartType == System.ServiceProcess.ServiceStartMode.Boot ||
                          service.StartType == System.ServiceProcess.ServiceStartMode.System;

            var settings = _settingsService.LoadSettings();
            if (settings.AiSummaries.TryGetValue(service.ServiceName, out var existingSummary))
            {
                AiSummaryList = existingSummary;
                HasAiSummary = true;
            }

            _ = LoadDetailsAsync(service);
        }
    }

    private async Task LoadDetailsAsync(WindowsServiceInfo service)
    {
        IsLoadingDetails = true;
        DetailedInfo = await Task.Run(() =>
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
        IsLoadingDetails = false;
    }

    [Reactive] public partial bool IsExecutingAction { get; set; }

    public IObservable<bool> CanStartService => 
        this.WhenAnyValue(x => x.Service, x => x.Service!.Status, 
            (svc, status) => svc != null && status == System.ServiceProcess.ServiceControllerStatus.Stopped);

    public IObservable<bool> CanStopService => 
        this.WhenAnyValue(x => x.Service, x => x.Service!.Status, 
            (svc, status) => svc != null && (status == System.ServiceProcess.ServiceControllerStatus.Running || status == System.ServiceProcess.ServiceControllerStatus.Paused));

    [ReactiveCommand(CanExecute = nameof(CanStartService))]
    public async Task StartService()
    {
        if (Service == null) return;
        IsExecutingAction = true;
        try
        {
            await Task.Run(() => _windowsServiceManager.Start(Service.ServiceName));
            await Task.Delay(1000); // Give it a moment to update state
            RefreshServiceState();
        }
        catch { /* Ignore or handle */ }
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
            await Task.Delay(1000); // Give it a moment to update state
            RefreshServiceState();
        }
        catch { /* Ignore or handle */ }
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
            await Task.Delay(1000); // Give it a moment to update state
            RefreshServiceState();
        }
        catch { /* Ignore or handle */ }
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
            await Task.Run(() => _windowsServiceManager.SetStartType(Service.ServiceName, System.ServiceProcess.ServiceStartMode.Disabled));
            IsAutoStart = false;
            Service.StartType = System.ServiceProcess.ServiceStartMode.Disabled;
        }
        catch { /* Ignore or handle */ }
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
            await Task.Run(() => _windowsServiceManager.SetStartType(Service.ServiceName, System.ServiceProcess.ServiceStartMode.Automatic));
            IsAutoStart = true;
            Service.StartType = System.ServiceProcess.ServiceStartMode.Automatic;
        }
        catch { /* Ignore or handle */ }
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
            // Just refresh the detailed info or status
            var updated = System.Linq.Enumerable.FirstOrDefault(_windowsServiceManager.GetServices(true), s => s.ServiceName == Service.ServiceName);
            if (updated != null)
            {
                Service.Status = updated.Status;
                Service.StartType = updated.StartType;
            }
        }
        catch { }
    }

    [ReactiveCommand]
    public void ToggleAiSummary()
    {
        IsAiSummaryExpanded = !IsAiSummaryExpanded;
        this.RaisePropertyChanged(nameof(ToggleAiSummaryText));
    }

    [ReactiveCommand]
    public async Task GenerateAiSummary()
    {
        if (DetailedInfo == null || Service == null) return;

        IsGeneratingSummary = true;
        HasAiSummary = false;
        IsAiSummaryExpanded = false;
        this.RaisePropertyChanged(nameof(ToggleAiSummaryText));

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

        var summaryList = await _serviceAnalyzer.AnalyzeServiceAsync(DetailedInfo);

        AiSummaryList = summaryList;
        HasAiSummary = true;
        IsGeneratingSummary = false;
        IsAiSummaryExpanded = true;
        this.RaisePropertyChanged(nameof(ToggleAiSummaryText));

        var settings = _settingsService.LoadSettings();
        settings.AiSummaries[Service.ServiceName] = summaryList;
        _settingsService.SaveSettings(settings);
    }
}