namespace MultiBloxy;

internal sealed record GuardAttemptResult(bool Success, string? Error = null);

internal sealed class RobloxGuardService : IDisposable
{
    private readonly object _syncRoot = new();
    private readonly string[] _guardNames;
    private readonly List<Mutex> _guards = [];

    public RobloxGuardService(params string[] guardNames)
    {
        ArgumentNullException.ThrowIfNull(guardNames);

        if (guardNames.Length == 0)
        {
            throw new ArgumentException(
                "At least one guard name is required.",
                nameof(guardNames));
        }

        foreach (string guardName in guardNames)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(guardName);
        }

        _guardNames = guardNames.ToArray();
    }

    public bool IsEnabled
    {
        get
        {
            lock (_syncRoot)
            {
                return _guards.Count == _guardNames.Length;
            }
        }
    }

    public GuardAttemptResult TryEnable()
    {
        lock (_syncRoot)
        {
            if (_guards.Count == _guardNames.Length)
            {
                return new GuardAttemptResult(Success: true);
            }

            DisableCore();

            List<Mutex> createdGuards = new(_guardNames.Length);
            try
            {
                foreach (string guardName in _guardNames)
                {
                    Mutex guard = new(
                        initiallyOwned: false,
                        name: guardName,
                        createdNew: out _);
                    createdGuards.Add(guard);
                }

                _guards.AddRange(createdGuards);
                return new GuardAttemptResult(Success: true);
            }
            catch (Exception exception) when (exception is IOException
                or UnauthorizedAccessException
                or WaitHandleCannotBeOpenedException)
            {
                foreach (Mutex guard in createdGuards)
                {
                    guard.Dispose();
                }

                return new GuardAttemptResult(Success: false, exception.Message);
            }
        }
    }

    public void Disable()
    {
        lock (_syncRoot)
        {
            DisableCore();
        }
    }

    private void DisableCore()
    {
        foreach (Mutex guard in _guards)
        {
            guard.Dispose();
        }

        _guards.Clear();
    }

    public void Dispose() => Disable();
}
