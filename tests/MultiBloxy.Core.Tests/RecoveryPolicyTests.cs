using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace MultiBloxy.Core.Tests;

[TestClass]
public sealed class RecoveryPolicyTests
{
    [DataTestMethod]
    [DataRow("Fix")]
    [DataRow("Abort")]
    [DataRow("Retry")]
    [DataRow("StopAllProcesses")]
    [DataRow("")]
    [DataRow(null)]
    public void ParsePersisted_DoesNotRestoreAutomaticActions(string? value)
    {
        Assert.AreEqual(
            MutexRecoveryAction.None,
            RecoveryPolicy.ParsePersisted(value));
    }

    [TestMethod]
    public void ParsePersisted_RestoresOnlyIgnore()
    {
        Assert.AreEqual(
            MutexRecoveryAction.Ignore,
            RecoveryPolicy.ParsePersisted("ignore"));
        Assert.IsTrue(RecoveryPolicy.CanRemember(MutexRecoveryAction.Ignore));
        Assert.IsFalse(RecoveryPolicy.CanRemember(MutexRecoveryAction.FixHandles));
        Assert.IsFalse(RecoveryPolicy.CanRemember(MutexRecoveryAction.StopAllProcesses));
    }
}
