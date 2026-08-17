using MultiBloxy.Core;

namespace MultiBloxy;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();

        FileLogger logger = new(AppConstants.LogDirectory);
        using AppSingleInstance singleInstance = AppSingleInstance.TryAcquire();
        if (!singleInstance.IsPrimary)
        {
            LocalizationService singletonLocalization = new();
            logger.Warning(
                singleInstance.Error is null
                    ? "A second MultiBloxy instance was rejected."
                    : $"The application singleton could not be created: {singleInstance.Error}");
            MessageBox.Show(
                singletonLocalization.Get(TextKeys.ErrorSingletonMessage),
                singletonLocalization.Get(TextKeys.ErrorSingletonCaption),
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
            return;
        }

        SettingsStore settingsStore = new(
            AppConstants.SettingsPath,
            AppConstants.LegacySettingsPath);
        SettingsLoadResult loadResult = settingsStore.Load();
        LocalizationService localization = new(loadResult.Settings.Language);

        Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
        Application.ThreadException += (_, eventArgs) =>
            HandleUnexpectedException(eventArgs.Exception, logger, localization);
        AppDomain.CurrentDomain.UnhandledException += (_, eventArgs) =>
        {
            Exception? exception = eventArgs.ExceptionObject as Exception;
            logger.Error("An unhandled AppDomain exception occurred.", exception);
        };

        try
        {
            using TrayApplicationContext context = new(
                settingsStore,
                loadResult,
                localization,
                logger);
            Application.Run(context);
        }
        catch (Exception exception)
        {
            HandleUnexpectedException(exception, logger, localization);
        }
    }

    private static void HandleUnexpectedException(
        Exception exception,
        FileLogger logger,
        LocalizationService localization)
    {
        logger.Error("An unexpected application error occurred.", exception);
        MessageBox.Show(
            localization.Format(TextKeys.ErrorUnexpectedMessage, logger.LogDirectory),
            localization.Get(TextKeys.ErrorUnexpectedCaption),
            MessageBoxButtons.OK,
            MessageBoxIcon.Error);
    }
}
