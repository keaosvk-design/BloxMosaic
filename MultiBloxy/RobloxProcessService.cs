using System.Diagnostics;

namespace MultiBloxy;

internal sealed record ProcessLaunchResult(bool Success, string? Error = null);

internal sealed record ProcessStopResult(
    int Found,
    int Stopped,
    int AlreadyExited,
    int Failed,
    bool Cancelled);

internal sealed class RobloxProcessService
{
    private static readonly TimeSpan GracefulCloseTimeout = TimeSpan.FromSeconds(3);
    private static readonly TimeSpan ForcedCloseTimeout = TimeSpan.FromSeconds(3);
    private readonly string _processName;

    public RobloxProcessService(string processName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(processName);
        _processName = processName;
    }

    public int GetProcessCount()
    {
        Process[] processes = Process.GetProcessesByName(_processName);
        try
        {
            return processes.Length;
        }
        finally
        {
            foreach (Process process in processes)
            {
                process.Dispose();
            }
        }
    }

    public ProcessLaunchResult StartRoblox() => OpenShellTarget("roblox-player:");

    public ProcessLaunchResult OpenWebAddress(string address)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(address);
        return OpenShellTarget(address);
    }

    public ProcessLaunchResult OpenInExplorer(string executablePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(executablePath);

        try
        {
            using Process? process = Process.Start(new ProcessStartInfo
            {
                FileName = "explorer.exe",
                Arguments = $"/select,\"{executablePath}\"",
                UseShellExecute = true,
            });
            return process is null
                ? new ProcessLaunchResult(Success: false, "Windows Explorer did not start.")
                : new ProcessLaunchResult(Success: true);
        }
        catch (Exception exception) when (exception is InvalidOperationException
            or System.ComponentModel.Win32Exception)
        {
            return new ProcessLaunchResult(Success: false, exception.Message);
        }
    }

    public ProcessLaunchResult OpenFolder(string folderPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(folderPath);

        try
        {
            Directory.CreateDirectory(folderPath);
            using Process? process = Process.Start(new ProcessStartInfo
            {
                FileName = folderPath,
                UseShellExecute = true,
            });
            return process is null
                ? new ProcessLaunchResult(Success: false, "Windows did not open the folder.")
                : new ProcessLaunchResult(Success: true);
        }
        catch (Exception exception) when (exception is IOException
            or UnauthorizedAccessException
            or InvalidOperationException
            or System.ComponentModel.Win32Exception)
        {
            return new ProcessLaunchResult(Success: false, exception.Message);
        }
    }

    public async Task<ProcessStopResult> StopAllAsync(CancellationToken cancellationToken)
    {
        Process[] processes = Process.GetProcessesByName(_processName);
        int stopped = 0;
        int alreadyExited = 0;
        int failed = 0;

        try
        {
            foreach (Process process in processes)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    break;
                }

                try
                {
                    if (process.HasExited)
                    {
                        alreadyExited++;
                        continue;
                    }

                    bool requestedGracefulClose = process.CloseMainWindow();
                    if (requestedGracefulClose
                        && await WaitForExitAsync(
                            process,
                            GracefulCloseTimeout,
                            cancellationToken).ConfigureAwait(true))
                    {
                        stopped++;
                        continue;
                    }

                    process.Kill(entireProcessTree: false);
                    if (await WaitForExitAsync(
                        process,
                        ForcedCloseTimeout,
                        cancellationToken).ConfigureAwait(true))
                    {
                        stopped++;
                    }
                    else
                    {
                        failed++;
                    }
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception exception) when (exception is InvalidOperationException
                    or System.ComponentModel.Win32Exception
                    or NotSupportedException)
                {
                    _ = exception;
                    failed++;
                }
            }
        }
        finally
        {
            foreach (Process process in processes)
            {
                process.Dispose();
            }
        }

        return new ProcessStopResult(
            Found: processes.Length,
            Stopped: stopped,
            AlreadyExited: alreadyExited,
            Failed: failed,
            Cancelled: cancellationToken.IsCancellationRequested);
    }

    private static async Task<bool> WaitForExitAsync(
        Process process,
        TimeSpan timeout,
        CancellationToken cancellationToken)
    {
        using CancellationTokenSource timeoutSource =
            CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutSource.CancelAfter(timeout);

        try
        {
            await process.WaitForExitAsync(timeoutSource.Token).ConfigureAwait(true);
            return true;
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                return process.HasExited;
            }
            catch (InvalidOperationException)
            {
                return true;
            }
        }
    }

    private static ProcessLaunchResult OpenShellTarget(string target)
    {
        try
        {
            using Process? process = Process.Start(new ProcessStartInfo
            {
                FileName = target,
                UseShellExecute = true,
            });
            return process is null
                ? new ProcessLaunchResult(Success: false, $"Windows did not open {target}.")
                : new ProcessLaunchResult(Success: true);
        }
        catch (Exception exception) when (exception is InvalidOperationException
            or System.ComponentModel.Win32Exception)
        {
            return new ProcessLaunchResult(Success: false, exception.Message);
        }
    }
}
