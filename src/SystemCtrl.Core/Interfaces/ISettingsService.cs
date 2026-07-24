using SystemCtrl.Core.Models;

namespace SystemCtrl.Core.Interfaces;

public interface ISettingsService
{
    AppSettings LoadSettings();
    void SaveSettings(AppSettings settings);
}
