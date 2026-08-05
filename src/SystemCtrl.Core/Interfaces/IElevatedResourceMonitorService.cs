using System;
using SystemCtrl.Core.Models;

namespace SystemCtrl.Core.Interfaces;

public interface IElevatedResourceMonitorService : IDisposable
{
    IObservable<ResourceStatsUpdate> MonitorProcess(int processId);
    void StopMonitoring();
}
