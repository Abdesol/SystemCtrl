using System;
using System.Runtime.InteropServices;

namespace SystemCtrl.Core.Interop;

[StructLayout(LayoutKind.Sequential)]
internal struct SERVICE_STATUS_PROCESS
{
    public int dwServiceType;
    public int dwCurrentState;
    public int dwControlsAccepted;
    public int dwWin32ExitCode;
    public int dwServiceSpecificExitCode;
    public int dwCheckPoint;
    public int dwWaitHint;
    public int dwProcessId;
    public int dwServiceFlags;
}

internal static class Advapi32
{
    [DllImport("advapi32.dll", SetLastError = true)]
    public static extern IntPtr OpenSCManager(string? machineName, string? databaseName, int access);

    [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Auto)]
    public static extern IntPtr OpenService(IntPtr hSCManager, string lpServiceName, int dwDesiredAccess);

    [DllImport("advapi32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool CloseServiceHandle(IntPtr hSCObject);

    [DllImport("advapi32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool QueryServiceStatusEx(IntPtr hService, int infoLevel, ref SERVICE_STATUS_PROCESS lpBuffer, int cbBufSize, out int pcbBytesNeeded);
}
