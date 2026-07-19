using SystemCtrl.Core.Models;

namespace SystemCtrl.Core.Interfaces;

public interface IWindowsServiceManager
{
    IEnumerable<WindowsServiceInfo> GetServices(bool includeSystemServices = false);

    void Start(string serviceName);

    void Stop(string serviceName);

    void Restart(string serviceName);
}