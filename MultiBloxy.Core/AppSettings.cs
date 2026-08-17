namespace MultiBloxy.Core;

public sealed record AppSettings
{
    public string? Language { get; init; }

    public bool PauseOnLaunch { get; init; }

    public MutexRecoveryAction RememberedAction { get; init; }

    public AppSettings Normalize()
    {
        string? language = Language?.Trim().ToLowerInvariant();
        if (language is not ("en" or "ru"))
        {
            language = null;
        }

        MutexRecoveryAction rememberedAction = RecoveryPolicy.CanRemember(RememberedAction)
            ? RememberedAction
            : MutexRecoveryAction.None;

        return this with
        {
            Language = language,
            RememberedAction = rememberedAction,
        };
    }
}
