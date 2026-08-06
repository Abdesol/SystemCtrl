using System.ServiceProcess;
using SystemCtrl.Core.Models;

namespace SystemCtrl.Core.Interfaces;

public interface IWindowsServiceManager
{
    IEnumerable<WindowsServiceInfo> GetServices(bool includeSystemServices = false);

    DetailedWindowsServiceInfo? GetDetailedInfo(string serviceName);

    void Start(string serviceName);

    void Stop(string serviceName);

    void Restart(string serviceName);

    void SetStartType(string serviceName, ServiceStartMode startMode);

    IEnumerable<string> GetLogs(string serviceName);
    IObservable<string> StreamLogs(string serviceName);
}