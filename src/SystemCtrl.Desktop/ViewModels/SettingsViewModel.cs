using System.Collections.ObjectModel;
using ReactiveUI;
using ReactiveUI.SourceGenerators;
using SystemCtrl.Core.Interfaces;
using SystemCtrl.Core.Models;
using SystemCtrl.Desktop.Services;
using System.Reflection;
using System.Linq;

namespace SystemCtrl.Desktop.ViewModels;

public partial class SettingsViewModel : ViewModelBase
{
    private readonly ISettingsService _settingsService;

    [Reactive]
    public partial string AppVersion { get; set; }

    [Reactive]
    public partial string ApiKey { get; set; }

    [Reactive]
    public partial string SelectedModel { get; set; }

    [Reactive]
    public partial bool ShowAiSummary { get; set; }

    [Reactive]
    public partial bool DisableServiceLogs { get; set; }
    
    [Reactive]
    public partial bool DisableResourceUsage { get; set; }

    [Reactive]
    public partial bool DisableScheduledTasksLogs { get; set; }

    [Reactive]
    public partial ObservableCollection<string> AvailableModels { get; set; }

    [Reactive]
    public partial string SelectedTheme { get; set; }

    [Reactive]
    public partial ObservableCollection<string> AvailableThemes { get; set; }

    public SettingsViewModel(ISettingsService settingsService)
    {
        _settingsService = settingsService;
        var settings = _settingsService.LoadSettings();
        
        ApiKey = settings.GeminiApiKey;
        SelectedModel = settings.GeminiModel;
        ShowAiSummary = settings.ShowAiSummary;
        DisableServiceLogs = settings.DisableServiceLogs;
        DisableResourceUsage = settings.DisableResourceUsage;
        DisableScheduledTasksLogs = settings.DisableScheduledTasksLogs;
        SelectedTheme = settings.AppTheme;

        var versionAttr = Assembly.GetExecutingAssembly()
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>();
        var rawVersion = versionAttr?.InformationalVersion ?? "1.0.0-dev";
        var plusIndex = rawVersion.IndexOf('+');
        var cleanVersion = plusIndex > 0 ? rawVersion.Substring(0, plusIndex) : rawVersion;
        AppVersion = "v" + cleanVersion;
        
        AvailableModels =
        [
            "gemini-3.6-flash",
            "gemini-3.5-flash",
            "gemini-3.5-flash-lite",
            "gemini-3.1-flash-lite",
            "gemini-2.5-flash"
        ];

        AvailableThemes =
        [
            "Light",
            "Dark"
        ];

        if (!AvailableModels.Contains(SelectedModel))
        {
            SelectedModel = AvailableModels[0];
        }

        if (!AvailableThemes.Contains(SelectedTheme))
        {
            SelectedTheme = AvailableThemes[0];
        }
    }

    public System.Action? OnSaved { get; set; }

    [ReactiveCommand]
    public void SaveSettings()
    {
        var settings = _settingsService.LoadSettings();
        settings.GeminiApiKey = ApiKey;
        settings.GeminiModel = SelectedModel;
        settings.ShowAiSummary = ShowAiSummary;
        settings.DisableServiceLogs = DisableServiceLogs;
        settings.DisableResourceUsage = DisableResourceUsage;
        settings.DisableScheduledTasksLogs = DisableScheduledTasksLogs;
        settings.AppTheme = SelectedTheme;
        _settingsService.SaveSettings(settings);

        App.SetTheme(SelectedTheme);

        OnSaved?.Invoke();
    }
}
