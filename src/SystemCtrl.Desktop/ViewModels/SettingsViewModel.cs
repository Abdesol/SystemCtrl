using System.Collections.ObjectModel;
using ReactiveUI;
using ReactiveUI.SourceGenerators;
using SystemCtrl.Core.Interfaces;
using SystemCtrl.Core.Models;
using SystemCtrl.Desktop.Services;

namespace SystemCtrl.Desktop.ViewModels;

public partial class SettingsViewModel : ViewModelBase
{
    private readonly ISettingsService _settingsService;

    [Reactive]
    public partial string ApiKey { get; set; }

    [Reactive]
    public partial string SelectedModel { get; set; }

    [Reactive]
    public partial ObservableCollection<string> AvailableModels { get; set; }

    public SettingsViewModel(ISettingsService settingsService)
    {
        _settingsService = settingsService;
        var settings = _settingsService.LoadSettings();
        
        ApiKey = settings.GeminiApiKey;
        SelectedModel = settings.GeminiModel;
        
        AvailableModels =
        [
            "gemini-3.6-flash",
            "gemini-3.1-pro",
            "gemini-3.5-flash",
            "gemini-3.5-flash-lite",
            "gemini-3.1-flash-lite",
            "gemini-2.5-flash",
            "gemini-2.5-flash-lite"
        ];

        if (!AvailableModels.Contains(SelectedModel))
        {
            SelectedModel = AvailableModels[0];
        }
    }

    public System.Action? OnSaved { get; set; }

    [ReactiveCommand]
    public void SaveSettings()
    {
        var settings = _settingsService.LoadSettings();
        settings.GeminiApiKey = ApiKey;
        settings.GeminiModel = SelectedModel;
        _settingsService.SaveSettings(settings);
        OnSaved?.Invoke();
    }
}
