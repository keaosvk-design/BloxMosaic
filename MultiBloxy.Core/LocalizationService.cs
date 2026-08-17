using System.Collections.ObjectModel;
using System.Globalization;

namespace MultiBloxy.Core;

public sealed class LocalizationService
{
    private static readonly IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> Locales =
        BuildLocales();

    public LocalizationService(string? requestedLocale = null)
    {
        SetLocale(requestedLocale);
    }

    public string CurrentLocale { get; private set; } = "en";

    public IReadOnlyCollection<string> SupportedLocales => Locales.Keys.ToArray();

    public void SetLocale(string? locale)
    {
        string? normalized = locale?.Trim().ToLowerInvariant();
        if (normalized is not null && Locales.ContainsKey(normalized))
        {
            CurrentLocale = normalized;
            return;
        }

        string detected = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
        CurrentLocale = Locales.ContainsKey(detected) ? detected : "en";
    }

    public string Get(string key)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        if (Locales[CurrentLocale].TryGetValue(key, out string? localized))
        {
            return localized;
        }

        return Locales["en"].TryGetValue(key, out string? english) ? english : key;
    }

    public string Format(string key, params object?[] arguments) =>
        string.Format(CultureInfo.CurrentCulture, Get(key), arguments);

    public string GetLocaleDisplayName(string locale)
    {
        CultureInfo culture = CultureInfo.GetCultureInfo(locale);
        return $"{culture.DisplayName} ({culture.NativeName})";
    }

    public IReadOnlyCollection<string> GetKeys(string locale)
    {
        if (!Locales.TryGetValue(locale, out IReadOnlyDictionary<string, string>? values))
        {
            return Array.Empty<string>();
        }

        return values.Keys.ToArray();
    }

    private static IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> BuildLocales()
    {
        Dictionary<string, string> english = new(StringComparer.Ordinal)
        {
            [TextKeys.StatusRunning] = "Status: Running",
            [TextKeys.StatusPaused] = "Status: Paused",
            [TextKeys.StatusBusy] = "Status: Working…",
            [TextKeys.StatusError] = "Status: Error",
            [TextKeys.MenuPause] = "Pause",
            [TextKeys.MenuResume] = "Resume",
            [TextKeys.MenuReload] = "Reload guard",
            [TextKeys.MenuStart] = "Start new Roblox instance",
            [TextKeys.MenuStopAll] = "Stop all Roblox instances…",
            [TextKeys.MenuShowInExplorer] = "Show application in Explorer",
            [TextKeys.MenuOpenLogs] = "Open logs folder",
            [TextKeys.MenuSettings] = "Settings",
            [TextKeys.MenuPauseOnLaunch] = "Pause on launch",
            [TextKeys.MenuResetRemembered] = "Reset remembered choice",
            [TextKeys.MenuLanguage] = "Language",
            [TextKeys.MenuAutoDetect] = "Auto detect",
            [TextKeys.MenuExit] = "Exit",
            [TextKeys.ErrorMutexCaption] = "Roblox guard could not be enabled",
            [TextKeys.ErrorMutexMessage] = "MultiBloxy could not create the Roblox guard. Roblox may already own an object with the same name. Choose one action. Automatic retries are intentionally limited.",
            [TextKeys.RecoveryFix] = "Close only matching handles in Roblox processes (advanced)",
            [TextKeys.RecoveryStopAll] = "Stop all Roblox processes",
            [TextKeys.RecoveryRetry] = "Try once again",
            [TextKeys.RecoveryIgnore] = "Continue with the guard disabled",
            [TextKeys.RecoveryRemember] = "Remember this choice",
            [TextKeys.RecoveryRememberHint] = "For safety, only ‘Continue with the guard disabled’ can be remembered.",
            [TextKeys.RecoveryConfirm] = "Continue",
            [TextKeys.RecoveryCancel] = "Cancel",
            [TextKeys.ErrorSingletonCaption] = "MultiBloxy is already running",
            [TextKeys.ErrorSingletonMessage] = "Another MultiBloxy instance is already running for this Windows user. Check the notification area.",
            [TextKeys.ErrorUnexpectedCaption] = "Unexpected MultiBloxy error",
            [TextKeys.ErrorUnexpectedMessage] = "MultiBloxy encountered an unexpected error. Details were written to the log folder:\n{0}",
            [TextKeys.ErrorOperationCaption] = "MultiBloxy operation failed",
            [TextKeys.ConfirmStopCaption] = "Stop Roblox processes?",
            [TextKeys.ConfirmStopMessage] = "MultiBloxy found {0} Roblox process(es). It will request a normal close first and force termination only when necessary. Active sessions will disconnect. Continue?",
            [TextKeys.ResultStop] = "Stopped: {0}; already exited: {1}; failed: {2}.",
            [TextKeys.ResultFix] = "Scanned {0} handle(s) in {1} Roblox process(es); closed {2}; failed to inspect {3}.",
            [TextKeys.ResultFinalFailure] = "The guard is still disabled after one recovery attempt. No further action was performed automatically.",
            [TextKeys.ResultSettingsWarning] = "Settings warning: {0}",
            [TextKeys.ResultNoProcesses] = "No Roblox processes were found.",
        };

        Dictionary<string, string> russian = new(StringComparer.Ordinal)
        {
            [TextKeys.StatusRunning] = "Статус: работает",
            [TextKeys.StatusPaused] = "Статус: приостановлено",
            [TextKeys.StatusBusy] = "Статус: выполняется операция…",
            [TextKeys.StatusError] = "Статус: ошибка",
            [TextKeys.MenuPause] = "Приостановить",
            [TextKeys.MenuResume] = "Возобновить",
            [TextKeys.MenuReload] = "Перезапустить защиту",
            [TextKeys.MenuStart] = "Запустить новый экземпляр Roblox",
            [TextKeys.MenuStopAll] = "Закрыть все экземпляры Roblox…",
            [TextKeys.MenuShowInExplorer] = "Показать приложение в Проводнике",
            [TextKeys.MenuOpenLogs] = "Открыть папку журналов",
            [TextKeys.MenuSettings] = "Настройки",
            [TextKeys.MenuPauseOnLaunch] = "Приостанавливать при запуске",
            [TextKeys.MenuResetRemembered] = "Сбросить запомненный выбор",
            [TextKeys.MenuLanguage] = "Язык",
            [TextKeys.MenuAutoDetect] = "Определять автоматически",
            [TextKeys.MenuExit] = "Выход",
            [TextKeys.ErrorMutexCaption] = "Не удалось включить защиту Roblox",
            [TextKeys.ErrorMutexMessage] = "MultiBloxy не смог создать объект защиты Roblox. Возможно, Roblox уже владеет объектом с тем же именем. Выберите одно действие. Автоматические повторы намеренно ограничены.",
            [TextKeys.RecoveryFix] = "Закрыть только совпадающие дескрипторы процессов Roblox (для опытных)",
            [TextKeys.RecoveryStopAll] = "Закрыть все процессы Roblox",
            [TextKeys.RecoveryRetry] = "Повторить один раз",
            [TextKeys.RecoveryIgnore] = "Продолжить с выключенной защитой",
            [TextKeys.RecoveryRemember] = "Запомнить этот выбор",
            [TextKeys.RecoveryRememberHint] = "В целях безопасности можно запомнить только продолжение с выключенной защитой.",
            [TextKeys.RecoveryConfirm] = "Продолжить",
            [TextKeys.RecoveryCancel] = "Отмена",
            [TextKeys.ErrorSingletonCaption] = "MultiBloxy уже запущен",
            [TextKeys.ErrorSingletonMessage] = "Для этого пользователя Windows уже запущен другой экземпляр MultiBloxy. Проверьте область уведомлений.",
            [TextKeys.ErrorUnexpectedCaption] = "Непредвиденная ошибка MultiBloxy",
            [TextKeys.ErrorUnexpectedMessage] = "В MultiBloxy произошла непредвиденная ошибка. Подробности записаны в папку журналов:\n{0}",
            [TextKeys.ErrorOperationCaption] = "Не удалось выполнить операцию MultiBloxy",
            [TextKeys.ConfirmStopCaption] = "Закрыть процессы Roblox?",
            [TextKeys.ConfirmStopMessage] = "MultiBloxy нашёл процессов Roblox: {0}. Сначала программа запросит обычное закрытие и применит принудительное завершение только при необходимости. Активные сессии будут отключены. Продолжить?",
            [TextKeys.ResultStop] = "Остановлено: {0}; уже завершилось: {1}; ошибок: {2}.",
            [TextKeys.ResultFix] = "Проверено дескрипторов: {0}, процессов Roblox: {1}; закрыто: {2}; ошибок проверки: {3}.",
            [TextKeys.ResultFinalFailure] = "После одной попытки восстановления защита всё ещё выключена. Дополнительные действия автоматически не выполнялись.",
            [TextKeys.ResultSettingsWarning] = "Предупреждение настроек: {0}",
            [TextKeys.ResultNoProcesses] = "Процессы Roblox не найдены.",
        };

        return new ReadOnlyDictionary<string, IReadOnlyDictionary<string, string>>(
            new Dictionary<string, IReadOnlyDictionary<string, string>>(StringComparer.OrdinalIgnoreCase)
            {
                ["en"] = new ReadOnlyDictionary<string, string>(english),
                ["ru"] = new ReadOnlyDictionary<string, string>(russian),
            });
    }
}
