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

    [Reactive] public partial string? AiSummary { get; set; }

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

        AiSummary = null;
        HasAiSummary = false;
        IsGeneratingSummary = false;
        IsAiSummaryExpanded = false;
        this.RaisePropertyChanged(nameof(ToggleAiSummaryText));

        if (service != null)
        {
            IsAutoStart = service.StartType == System.ServiceProcess.ServiceStartMode.Automatic ||
                          service.StartType == System.ServiceProcess.ServiceStartMode.Boot ||
                          service.StartType == System.ServiceProcess.ServiceStartMode.System;

            var settings = _settingsService.LoadSettings();
            if (settings.AiSummaries.TryGetValue(service.ServiceName, out var existingSummary))
            {
                AiSummary = existingSummary;
                HasAiSummary = true;
            }
        }

        Task.Run(() =>
        {
            try
            {
                return _windowsServiceManager.GetDetailedInfo(service.ServiceName);
            }
            catch
            {
                return null;
            }
        }).ContinueWith(t =>
        {
            DetailedInfo = t.Result;
            IsLoadingDetails = false;
        }, TaskScheduler.FromCurrentSynchronizationContext());
    }

    [ReactiveCommand]
    public void StartService()
    {
        if (Service == null) return;
        _windowsServiceManager.Start(Service.ServiceName);
    }

    [ReactiveCommand]
    public void StopService()
    {
        if (Service == null) return;
        _windowsServiceManager.Stop(Service.ServiceName);
    }

    [ReactiveCommand]
    public void RestartService()
    {
        if (Service == null) return;
        _windowsServiceManager.Restart(Service.ServiceName);
    }

    [ReactiveCommand]
    public void DisableAutoStart()
    {
        if (Service == null) return;
        _windowsServiceManager.SetStartType(Service.ServiceName, System.ServiceProcess.ServiceStartMode.Disabled);
        IsAutoStart = false;
        Service.StartType = System.ServiceProcess.ServiceStartMode.Disabled;
        this.RaisePropertyChanged(nameof(Service));
    }

    [ReactiveCommand]
    public void EnableAutoStart()
    {
        if (Service == null) return;
        _windowsServiceManager.SetStartType(Service.ServiceName, System.ServiceProcess.ServiceStartMode.Automatic);
        IsAutoStart = true;
        Service.StartType = System.ServiceProcess.ServiceStartMode.Automatic;
        this.RaisePropertyChanged(nameof(Service));
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
            int i = 0;
            while (IsGeneratingSummary)
            {
                LoadingSummaryText = loadingTexts[i % loadingTexts.Length];
                i++;
                await Task.Delay(TimeSpan.FromSeconds(2));
            }
        });

        var summary = await _serviceAnalyzer.AnalyzeServiceAsync(DetailedInfo);

        AiSummary = summary;
        HasAiSummary = true;
        IsGeneratingSummary = false;
        IsAiSummaryExpanded = true;
        this.RaisePropertyChanged(nameof(ToggleAiSummaryText));

        var settings = _settingsService.LoadSettings();
        settings.AiSummaries[Service.ServiceName] = summary;
        _settingsService.SaveSettings(settings);
    }
}