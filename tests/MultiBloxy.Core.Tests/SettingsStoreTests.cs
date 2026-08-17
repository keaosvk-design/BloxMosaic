using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace MultiBloxy.Core.Tests;

[TestClass]
public sealed class SettingsStoreTests
{
    private string _temporaryDirectory = null!;

    [TestInitialize]
    public void Initialize()
    {
        _temporaryDirectory = Path.Combine(
            Path.GetTempPath(),
            "MultiBloxy.Tests",
            Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_temporaryDirectory);
    }

    [TestCleanup]
    public void Cleanup()
    {
        if (Directory.Exists(_temporaryDirectory))
        {
            Directory.Delete(_temporaryDirectory, recursive: true);
        }
    }

    [TestMethod]
    public void SaveAndLoad_RoundTripsSettings()
    {
        string path = Path.Combine(_temporaryDirectory, "config.xml");
        SettingsStore store = new(path);
        AppSettings expected = new()
        {
            Language = "ru",
            PauseOnLaunch = true,
            RememberedAction = MutexRecoveryAction.Ignore,
        };

        SettingsSaveResult saveResult = store.Save(expected);
        SettingsLoadResult loadResult = store.Load();

        Assert.IsTrue(saveResult.Success, saveResult.Error);
        Assert.AreEqual(expected, loadResult.Settings);
        Assert.IsNull(loadResult.Warning);
        Assert.IsFalse(Directory.EnumerateFiles(_temporaryDirectory, "*.tmp").Any());
    }

    [TestMethod]
    public void Load_MigratesLegacyConfigWithoutDeletingIt()
    {
        string currentPath = Path.Combine(_temporaryDirectory, "new", "config.xml");
        string legacyPath = Path.Combine(_temporaryDirectory, "legacy.xml");
        File.WriteAllText(legacyPath, "<Config><Language>ru</Language></Config>");
        SettingsStore store = new(currentPath, legacyPath);

        SettingsLoadResult result = store.Load();

        Assert.IsTrue(result.Migrated);
        Assert.AreEqual("ru", result.Settings.Language);
        Assert.IsTrue(File.Exists(currentPath));
        Assert.IsTrue(File.Exists(legacyPath));
    }

    [TestMethod]
    public void Load_QuarantinesCorruptCurrentConfig()
    {
        string path = Path.Combine(_temporaryDirectory, "config.xml");
        File.WriteAllText(path, "<Config>");
        SettingsStore store = new(path);

        SettingsLoadResult result = store.Load();

        Assert.AreEqual(new AppSettings(), result.Settings);
        Assert.IsNotNull(result.Warning);
        Assert.IsFalse(File.Exists(path));
        Assert.AreEqual(
            1,
            Directory.EnumerateFiles(_temporaryDirectory, "config.corrupt-*.xml").Count());
    }
}
