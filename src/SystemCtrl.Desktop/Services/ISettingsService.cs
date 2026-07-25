using SystemCtrl.Core.Models;

namespace SystemCtrl.Desktop.Services;

public interface ISettingsService
{
    AppSettings LoadSettings();
    void SaveSettings(AppSettings settings);
}
