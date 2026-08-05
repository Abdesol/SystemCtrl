using System;
using System.Diagnostics;
using System.IO.Pipes;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using SystemCtrl.Core.Models;

namespace SystemCtrl.Desktop;

public static class ElevatedResourceHelper
{
    public static void Run(string[] args)
    {
        if (args.Length < 2) return;
        var pipeName = args[1];

        using var pipeClient = new NamedPipeClientStream(".", pipeName, PipeDirection.InOut, PipeOptions.Asynchronous);
        try
        {
            pipeClient.Connect(5000); // 5 seconds timeout
        }
        catch
        {
            return;
        }

        using var reader = new System.IO.StreamReader(pipeClient);
        using var writer = new System.IO.StreamWriter(pipeClient) { AutoFlush = true };

        int? currentProcessId = null;
        var cts = new CancellationTokenSource();

        // Background task to read requests from the main app
        _ = Task.Run(async () =>
        {
            try
            {
                while (!cts.IsCancellationRequested)
                {
                    var line = await reader.ReadLineAsync();
                    if (line == null) break; // Pipe broken

                    if (int.TryParse(line, out var pid))
                    {
                        currentProcessId = pid;
                    }
                }
            }
            catch
            {
                // ignored
            }
            finally
            {
                cts.Cancel();
            }
        });

        // Loop to poll stats and send them back
        while (!cts.IsCancellationRequested)
        {
            if (currentProcessId.HasValue)
            {
                var pid = currentProcessId.Value;
                var update = GetStats(pid);
                var json = JsonSerializer.Serialize(update);
                try
                {
                    writer.WriteLine(json);
                }
                catch
                {
                    break;
                }
            }

            try
            {
                Task.Delay(1000, cts.Token).Wait();
            }
            catch
            {
                break;
            }
        }
    }

    private static ResourceStatsUpdate GetStats(int pid)
    {
        var update = new ResourceStatsUpdate { ProcessId = pid };
        try
        {
            using var proc = Process.GetProcessById(pid);

            update.MemoryBytes = proc.WorkingSet64;
            
            try { update.RunningFor = DateTime.Now - proc.StartTime; } catch { /* access denied */ }

            try
            {
                var t1 = proc.TotalProcessorTime;
                var w1 = DateTime.UtcNow;
                Thread.Sleep(500);
                proc.Refresh();
                var t2 = proc.TotalProcessorTime;
                var w2 = DateTime.UtcNow;

                var cpuUsed = (t2 - t1).TotalMilliseconds;
                var elapsed = (w2 - w1).TotalMilliseconds;
                var logicalCores = Environment.ProcessorCount;

                var cpu = Math.Round(cpuUsed / (elapsed * logicalCores) * 100.0, 1);
                update.CpuPercent = Math.Min(cpu, 100.0);
            }
            catch { /* access denied or process exited */ }
        }
        catch (Exception ex)
        {
            update.Error = ex.Message;
        }

        return update;
    }
}
