namespace MultiBloxy;

internal sealed record GuardAttemptResult(bool Success, string? Error = null);

internal sealed class RobloxGuardService : IDisposable
{
    private readonly object _syncRoot = new();
    private readonly string _guardName;
    private Mutex? _guard;

    public RobloxGuardService(string guardName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(guardName);
        _guardName = guardName;
    }

    public bool IsEnabled
    {
        get
        {
            lock (_syncRoot)
            {
                return _guard is not null;
            }
        }
    }

    public GuardAttemptResult TryEnable()
    {
        lock (_syncRoot)
        {
            if (_guard is not null)
            {
                return new GuardAttemptResult(Success: true);
            }

            try
            {
                _guard = new Mutex(
                    initiallyOwned: false,
                    name: _guardName,
                    createdNew: out _);
                return new GuardAttemptResult(Success: true);
            }
            catch (Exception exception) when (exception is IOException
                or UnauthorizedAccessException
                or WaitHandleCannotBeOpenedException)
            {
                _guard = null;
                return new GuardAttemptResult(Success: false, exception.Message);
            }
        }
    }

    public void Disable()
    {
        lock (_syncRoot)
        {
            _guard?.Dispose();
            _guard = null;
        }
    }

    public void Dispose() => Disable();
}
