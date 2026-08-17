namespace MultiBloxy.Core;

public enum MutexRecoveryAction
{
    None,
    FixHandles,
    StopAllProcesses,
    Retry,
    Ignore,
}
