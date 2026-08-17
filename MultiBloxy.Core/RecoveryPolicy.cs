namespace MultiBloxy.Core;

public static class RecoveryPolicy
{
    public static bool CanRemember(MutexRecoveryAction action) =>
        action == MutexRecoveryAction.Ignore;

    public static bool RequiresAnotherEnableAttempt(MutexRecoveryAction action) =>
        action is MutexRecoveryAction.FixHandles
            or MutexRecoveryAction.StopAllProcesses
            or MutexRecoveryAction.Retry;

    public static bool IsDestructive(MutexRecoveryAction action) =>
        action is MutexRecoveryAction.FixHandles
            or MutexRecoveryAction.StopAllProcesses;

    public static MutexRecoveryAction ParsePersisted(string? value)
    {
        // Older versions persisted Fix, Abort and Retry. Running any of those
        // automatically is unsafe, so only the non-destructive Ignore choice migrates.
        return string.Equals(value?.Trim(), "Ignore", StringComparison.OrdinalIgnoreCase)
            ? MutexRecoveryAction.Ignore
            : MutexRecoveryAction.None;
    }

    public static string? ToPersisted(MutexRecoveryAction action) =>
        CanRemember(action) ? "Ignore" : null;
}
