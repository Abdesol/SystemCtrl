using System;
using System.IO;
using System.Text.Json;
using SystemCtrl.Core.Interfaces;
using SystemCtrl.Core.Models;

namespace SystemCtrl.Desktop.Services;

public class SettingsService : ISettingsService
{
    private readonly string _settingsFilePath;

    public SettingsService()
    {
        var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var appFolder = Path.Combine(appDataPath, "SystemCtrl");
        if (!Directory.Exists(appFolder))
        {
            Directory.CreateDirectory(appFolder);
        }
        _settingsFilePath = Path.Combine(appFolder, "settings.json");
    }

    public AppSettings LoadSettings()
    {
        if (!File.Exists(_settingsFilePath))
        {
            return new AppSettings();
        }

        try
        {
            var json = File.ReadAllText(_settingsFilePath);
            return JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
        }
        catch
        {
            return new AppSettings();
        }
    }

    public void SaveSettings(AppSettings settings)
    {
        try
        {
            var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_settingsFilePath, json);
        }
        catch
        {
            // Ignore for now
        }
    }
}
