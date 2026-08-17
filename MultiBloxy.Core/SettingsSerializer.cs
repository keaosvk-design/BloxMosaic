using System.Text;
using System.Xml;
using System.Xml.Linq;

namespace MultiBloxy.Core;

public static class SettingsSerializer
{
    public static bool TryDeserialize(
        string xml,
        out AppSettings settings,
        out string? error)
    {
        ArgumentNullException.ThrowIfNull(xml);

        try
        {
            XmlReaderSettings readerSettings = new()
            {
                DtdProcessing = DtdProcessing.Prohibit,
                XmlResolver = null,
            };

            using StringReader stringReader = new(xml);
            using XmlReader reader = XmlReader.Create(stringReader, readerSettings);
            XDocument document = XDocument.Load(reader, LoadOptions.None);

            XElement? root = document.Root;
            if (root is null || root.Name != "Config")
            {
                settings = new AppSettings();
                error = "The settings file must contain a Config root element.";
                return false;
            }

            bool pauseOnLaunch = bool.TryParse(
                root.Element("PauseOnLaunch")?.Value,
                out bool parsedPause) && parsedPause;

            settings = new AppSettings
            {
                Language = root.Element("Language")?.Value,
                PauseOnLaunch = pauseOnLaunch,
                RememberedAction = RecoveryPolicy.ParsePersisted(
                    root.Element("MutexErrorAction")?.Value),
            }.Normalize();

            error = null;
            return true;
        }
        catch (Exception exception) when (exception is XmlException or InvalidOperationException)
        {
            settings = new AppSettings();
            error = exception.Message;
            return false;
        }
    }

    public static string Serialize(AppSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        AppSettings normalized = settings.Normalize();

        XElement root = new("Config");
        if (normalized.Language is not null)
        {
            root.Add(new XElement("Language", normalized.Language));
        }

        if (normalized.PauseOnLaunch)
        {
            root.Add(new XElement("PauseOnLaunch", true));
        }

        string? persistedAction = RecoveryPolicy.ToPersisted(normalized.RememberedAction);
        if (persistedAction is not null)
        {
            root.Add(new XElement("MutexErrorAction", persistedAction));
        }

        XDocument document = new(
            new XDeclaration("1.0", "utf-8", null),
            root);

        using MemoryStream stream = new();
        XmlWriterSettings writerSettings = new()
        {
            Encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false),
            Indent = true,
            OmitXmlDeclaration = false,
        };

        using (XmlWriter writer = XmlWriter.Create(stream, writerSettings))
        {
            document.Save(writer);
        }

        return Encoding.UTF8.GetString(stream.ToArray());
    }
}
