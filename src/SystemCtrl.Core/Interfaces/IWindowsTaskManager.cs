using SystemCtrl.Core.Models;

namespace SystemCtrl.Core.Interfaces;

public interface IWindowsTaskManager
{
    IEnumerable<WindowsTaskInfo> GetTasks(bool includeSystemTasks = false);

    DetailedWindowsTaskInfo GetDetailedInfo(string taskPath);

    void Enable(string taskPath);

    void Disable(string taskPath);

    void Run(string taskPath);

    void Stop(string taskPath);
}
