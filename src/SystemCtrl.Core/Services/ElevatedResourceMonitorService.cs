using System;
using System.IO.Pipes;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;
using SystemCtrl.Core.Models;
using SystemCtrl.Core.Interfaces;
using System.Reactive.Subjects;
using System.Reactive.Linq;

namespace SystemCtrl.Core.Services;


public class ElevatedResourceMonitorService : IElevatedResourceMonitorService
{
    private readonly IAdminService _adminService;
    private NamedPipeServerStream? _pipeServer;
    private System.IO.StreamReader? _reader;
    private System.IO.StreamWriter? _writer;
    private Process? _helperProcess;
    private readonly Subject<ResourceStatsUpdate> _updates = new();
    private int? _currentProcessId;
    private CancellationTokenSource? _cts;
    private readonly object _lock = new();

    public ElevatedResourceMonitorService(IAdminService adminService)
    {
        _adminService = adminService;
    }

    public IObservable<ResourceStatsUpdate> MonitorProcess(int processId)
    {
        lock (_lock)
        {
            EnsureHelperRunning();
            _currentProcessId = processId;
            _writer?.WriteLine(processId.ToString());
            return _updates.AsObservable();
        }
    }

    public void StopMonitoring()
    {
        lock (_lock)
        {
            _currentProcessId = null;
        }
    }

    private void EnsureHelperRunning()
    {
        if (_pipeServer != null && _pipeServer.IsConnected)
        {
            return;
        }

        DisposeHelper();

        var pipeName = "SystemCtrl_ElevatedMonitor_" + Guid.NewGuid().ToString("N");
        _pipeServer = new NamedPipeServerStream(pipeName, PipeDirection.InOut, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous);

        var exePath = Process.GetCurrentProcess().MainModule?.FileName;
        if (string.IsNullOrEmpty(exePath))
        {
            throw new Exception("Unable to determine current executable path.");
        }

        var processInfo = new ProcessStartInfo
        {
            FileName = exePath,
            Arguments = $"--elevated-helper {pipeName}",
            UseShellExecute = true,
            WindowStyle = ProcessWindowStyle.Hidden
        };

        if (!_adminService.IsRunningAsAdmin())
        {
            processInfo.Verb = "runas";
        }

        _helperProcess = _adminService.StartElevatedProcess(processInfo);

        _cts = new CancellationTokenSource();

        Task.Run(async () =>
        {
            try
            {
                await _pipeServer.WaitForConnectionAsync(_cts.Token);
                _reader = new System.IO.StreamReader(_pipeServer);
                _writer = new System.IO.StreamWriter(_pipeServer) { AutoFlush = true };

                if (_currentProcessId.HasValue)
                {
                    _writer.WriteLine(_currentProcessId.Value.ToString());
                }

                while (!_cts.Token.IsCancellationRequested && _pipeServer.IsConnected)
                {
                    var line = await _reader.ReadLineAsync();
                    if (line == null) break;

                    try
                    {
                        var update = JsonSerializer.Deserialize<ResourceStatsUpdate>(line);
                        if (update != null && update.ProcessId == _currentProcessId)
                        {
                            _updates.OnNext(update);
                        }
                    }
                    catch
                    {
                        // ignore deserialization errors
                    }
                }
            }
            catch
            {
                // ignored
            }
            finally
            {
                DisposeHelper();
            }
        });
    }

    private void DisposeHelper()
    {
        try { _cts?.Cancel(); } catch { }
        try { _reader?.Dispose(); } catch { }
        try { _writer?.Dispose(); } catch { }
        try { _pipeServer?.Dispose(); } catch { }
        
        if (_helperProcess != null && !_helperProcess.HasExited)
        {
            try { _helperProcess.Kill(); } catch { }
        }
        try { _helperProcess?.Dispose(); } catch { }

        _pipeServer = null;
        _reader = null;
        _writer = null;
        _helperProcess = null;
        _cts = null;
    }

    public void Dispose()
    {
        DisposeHelper();
        _updates.Dispose();
    }
}
