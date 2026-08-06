using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace SystemCtrl.Core.Interfaces;

public interface IAdminService
{
    bool IsRunningAsAdmin();
    Process? StartElevatedProcess(ProcessStartInfo processInfo);
}
