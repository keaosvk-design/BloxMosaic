using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace MultiBloxy.Core.Tests;

[TestClass]
public sealed class LocalizationServiceTests
{
    [TestMethod]
    public void EveryLocaleContainsTheSameKeys()
    {
        LocalizationService localization = new("en");
        string[] englishKeys = localization.GetKeys("en").Order().ToArray();

        foreach (string locale in localization.SupportedLocales)
        {
            CollectionAssert.AreEqual(
                englishKeys,
                localization.GetKeys(locale).Order().ToArray(),
                $"Locale {locale} does not contain the same keys as English.");
        }
    }

    [TestMethod]
    public void MissingKeyFallsBackToTheKey()
    {
        LocalizationService localization = new("ru");

        Assert.AreEqual("Missing.Test.Key", localization.Get("Missing.Test.Key"));
    }

    [TestMethod]
    public void ExplicitLocaleCanBeChanged()
    {
        LocalizationService localization = new("en");

        localization.SetLocale("ru");

        Assert.AreEqual("ru", localization.CurrentLocale);
        Assert.AreEqual("Выход", localization.Get(TextKeys.MenuExit));
    }
}
