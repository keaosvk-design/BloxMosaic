using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace MultiBloxy.Core.Tests;

[TestClass]
public sealed class SettingsSerializerTests
{
    [TestMethod]
    public void TryDeserialize_ReadsValidLegacySettings()
    {
        const string xml = """
            <?xml version="1.0" encoding="utf-8"?>
            <Config>
              <Language>ru</Language>
              <PauseOnLaunch>true</PauseOnLaunch>
              <MutexErrorAction>Ignore</MutexErrorAction>
            </Config>
            """;

        bool success = SettingsSerializer.TryDeserialize(
            xml,
            out AppSettings settings,
            out string? error);

        Assert.IsTrue(success, error);
        Assert.AreEqual("ru", settings.Language);
        Assert.IsTrue(settings.PauseOnLaunch);
        Assert.AreEqual(MutexRecoveryAction.Ignore, settings.RememberedAction);
    }

    [TestMethod]
    public void TryDeserialize_DropsLegacyAbortAction()
    {
        const string xml = "<Config><MutexErrorAction>Abort</MutexErrorAction></Config>";

        bool success = SettingsSerializer.TryDeserialize(
            xml,
            out AppSettings settings,
            out string? error);

        Assert.IsTrue(success, error);
        Assert.AreEqual(MutexRecoveryAction.None, settings.RememberedAction);
    }

    [TestMethod]
    [DataRow("<Config>")]
    [DataRow("<WrongRoot />")]
    [DataRow("<!DOCTYPE Config [<!ENTITY xxe SYSTEM 'file:///etc/passwd'>]><Config>&xxe;</Config>")]
    public void TryDeserialize_RejectsMalformedOrUnsafeXml(string xml)
    {
        bool success = SettingsSerializer.TryDeserialize(
            xml,
            out AppSettings settings,
            out string? error);

        Assert.IsFalse(success);
        Assert.IsNotNull(error);
        Assert.AreEqual(new AppSettings(), settings);
    }

    [TestMethod]
    public void Serialize_RoundTripsNormalizedSettings()
    {
        AppSettings expected = new()
        {
            Language = "en",
            PauseOnLaunch = true,
            RememberedAction = MutexRecoveryAction.Ignore,
        };

        string xml = SettingsSerializer.Serialize(expected);
        bool success = SettingsSerializer.TryDeserialize(
            xml,
            out AppSettings actual,
            out string? error);

        Assert.IsTrue(success, error);
        Assert.AreEqual(expected, actual);
        StringAssert.StartsWith(xml, "<?xml version=\"1.0\" encoding=\"utf-8\"?>");
    }
}
