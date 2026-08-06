using System;
using System.Diagnostics;
using System.IO;
using System.Security.Principal;
using System.Threading.Tasks;
using SystemCtrl.Core.Interfaces;
using SystemCtrl.Core.Exceptions;

namespace SystemCtrl.Core;

public class AdminService : IAdminService
{
    public bool IsRunningAsAdmin()
    {
        using var identity = WindowsIdentity.GetCurrent();
        var principal = new WindowsPrincipal(identity);
        return principal.IsInRole(WindowsBuiltInRole.Administrator);
    }

    public Process? StartElevatedProcess(ProcessStartInfo processInfo)
    {
        if (!IsRunningAsAdmin())
        {
            processInfo.Verb = "runas";
            processInfo.UseShellExecute = true;
        }

        return Process.Start(processInfo);
    }
}
