using System.Text;

namespace MultiBloxy;

internal sealed class FileLogger
{
    private const long MaximumLogBytes = 1_048_576;
    private readonly object _syncRoot = new();
    private readonly string _logPath;

    public FileLogger(string logDirectory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(logDirectory);
        LogDirectory = Path.GetFullPath(logDirectory);
        _logPath = Path.Combine(LogDirectory, "MultiBloxy.log");
    }

    public string LogDirectory { get; }

    public string LogPath => _logPath;

    public void Info(string message) => Write("INFO", message, null);

    public void Warning(string message) => Write("WARN", message, null);

    public void Error(string message, Exception? exception = null) =>
        Write("ERROR", message, exception);

    private void Write(string level, string message, Exception? exception)
    {
        try
        {
            lock (_syncRoot)
            {
                Directory.CreateDirectory(LogDirectory);
                RotateIfNeeded();

                StringBuilder entry = new();
                entry.Append(DateTimeOffset.UtcNow.ToString("O", System.Globalization.CultureInfo.InvariantCulture));
                entry.Append(" [");
                entry.Append(level);
                entry.Append("] ");
                entry.AppendLine(message.ReplaceLineEndings(" "));
                if (exception is not null)
                {
                    entry.AppendLine(exception.ToString());
                }

                File.AppendAllText(_logPath, entry.ToString(), new UTF8Encoding(false));
            }
        }
        catch (Exception logException) when (logException is IOException
            or UnauthorizedAccessException
            or NotSupportedException)
        {
            // Logging must never crash the tray application.
            _ = logException;
        }
    }

    private void RotateIfNeeded()
    {
        FileInfo file = new(_logPath);
        if (!file.Exists || file.Length < MaximumLogBytes)
        {
            return;
        }

        string previousLogPath = Path.Combine(LogDirectory, "MultiBloxy.previous.log");
        File.Move(_logPath, previousLogPath, overwrite: true);
    }
}
