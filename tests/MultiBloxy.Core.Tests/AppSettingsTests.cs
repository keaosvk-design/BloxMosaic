using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace MultiBloxy.Core.Tests;

[TestClass]
public sealed class AppSettingsTests
{
    [TestMethod]
    public void Normalize_AcceptsSupportedLanguageAndIgnoreAction()
    {
        AppSettings settings = new()
        {
            Language = " RU ",
            PauseOnLaunch = true,
            RememberedAction = MutexRecoveryAction.Ignore,
        };

        AppSettings normalized = settings.Normalize();

        Assert.AreEqual("ru", normalized.Language);
        Assert.IsTrue(normalized.PauseOnLaunch);
        Assert.AreEqual(MutexRecoveryAction.Ignore, normalized.RememberedAction);
    }

    [TestMethod]
    public void Normalize_DropsUnsupportedLanguageAndDestructiveAction()
    {
        AppSettings settings = new()
        {
            Language = "de",
            RememberedAction = MutexRecoveryAction.StopAllProcesses,
        };

        AppSettings normalized = settings.Normalize();

        Assert.IsNull(normalized.Language);
        Assert.AreEqual(MutexRecoveryAction.None, normalized.RememberedAction);
    }
}
