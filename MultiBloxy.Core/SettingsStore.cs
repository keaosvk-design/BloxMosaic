using System.Text;

namespace MultiBloxy.Core;

public sealed record SettingsLoadResult(
    AppSettings Settings,
    bool Migrated,
    string? Warning = null);

public sealed record SettingsSaveResult(bool Success, string? Error = null);

public sealed class SettingsStore
{
    private static readonly UTF8Encoding Utf8WithoutBom = new(false);

    private readonly string _settingsPath;
    private readonly string? _legacySettingsPath;

    public SettingsStore(string settingsPath, string? legacySettingsPath = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(settingsPath);
        _settingsPath = Path.GetFullPath(settingsPath);
        _legacySettingsPath = string.IsNullOrWhiteSpace(legacySettingsPath)
            ? null
            : Path.GetFullPath(legacySettingsPath);
    }

    public string SettingsPath => _settingsPath;

    public SettingsLoadResult Load()
    {
        if (File.Exists(_settingsPath))
        {
            return LoadExisting(_settingsPath, quarantineOnFailure: true, migrated: false);
        }

        if (_legacySettingsPath is not null && File.Exists(_legacySettingsPath))
        {
            SettingsLoadResult legacyResult = LoadExisting(
                _legacySettingsPath,
                quarantineOnFailure: false,
                migrated: true);

            if (legacyResult.Warning is not null)
            {
                return legacyResult;
            }

            SettingsSaveResult saveResult = Save(legacyResult.Settings);
            return saveResult.Success
                ? legacyResult
                : legacyResult with
                {
                    Warning = $"Legacy settings were read but could not be migrated: {saveResult.Error}",
                };
        }

        return new SettingsLoadResult(new AppSettings(), Migrated: false);
    }

    public SettingsSaveResult Save(AppSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        string directory = Path.GetDirectoryName(_settingsPath)
            ?? throw new InvalidOperationException("The settings path has no parent directory.");
        string temporaryPath = Path.Combine(
            directory,
            $".{Path.GetFileName(_settingsPath)}.{Guid.NewGuid():N}.tmp");

        try
        {
            Directory.CreateDirectory(directory);
            File.WriteAllText(temporaryPath, SettingsSerializer.Serialize(settings), Utf8WithoutBom);

            if (File.Exists(_settingsPath))
            {
                try
                {
                    File.Replace(temporaryPath, _settingsPath, destinationBackupFileName: null);
                }
                catch (PlatformNotSupportedException)
                {
                    File.Move(temporaryPath, _settingsPath, overwrite: true);
                }
            }
            else
            {
                File.Move(temporaryPath, _settingsPath);
            }

            return new SettingsSaveResult(Success: true);
        }
        catch (Exception exception) when (exception is IOException
            or UnauthorizedAccessException
            or NotSupportedException)
        {
            return new SettingsSaveResult(Success: false, exception.Message);
        }
        finally
        {
            try
            {
                if (File.Exists(temporaryPath))
                {
                    File.Delete(temporaryPath);
                }
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
            {
                // The next save uses a unique name, so an undeletable stale temp file is harmless.
                _ = exception;
            }
        }
    }

    public static string GetDefaultPath(string applicationName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(applicationName);
        string localData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        return Path.Combine(localData, applicationName, "config.xml");
    }

    private static SettingsLoadResult LoadExisting(
        string path,
        bool quarantineOnFailure,
        bool migrated)
    {
        try
        {
            string xml = File.ReadAllText(path, Utf8WithoutBom);
            if (SettingsSerializer.TryDeserialize(xml, out AppSettings settings, out string? error))
            {
                return new SettingsLoadResult(settings, migrated);
            }

            string? quarantinePath = quarantineOnFailure ? TryQuarantine(path) : null;
            string warning = quarantinePath is null
                ? $"Invalid settings were ignored: {error}"
                : $"Invalid settings were moved to {quarantinePath}: {error}";
            return new SettingsLoadResult(new AppSettings(), Migrated: false, warning);
        }
        catch (Exception exception) when (exception is IOException
            or UnauthorizedAccessException
            or NotSupportedException)
        {
            return new SettingsLoadResult(
                new AppSettings(),
                Migrated: false,
                $"Settings could not be read: {exception.Message}");
        }
    }

    private static string? TryQuarantine(string path)
    {
        try
        {
            string directory = Path.GetDirectoryName(path) ?? string.Empty;
            string fileName = Path.GetFileNameWithoutExtension(path);
            string extension = Path.GetExtension(path);
            string quarantinePath = Path.Combine(
                directory,
                $"{fileName}.corrupt-{DateTime.UtcNow:yyyyMMdd-HHmmss}-{Guid.NewGuid():N}{extension}");
            File.Move(path, quarantinePath);
            return quarantinePath;
        }
        catch (Exception exception) when (exception is IOException
            or UnauthorizedAccessException
            or NotSupportedException)
        {
            _ = exception;
            return null;
        }
    }
}
