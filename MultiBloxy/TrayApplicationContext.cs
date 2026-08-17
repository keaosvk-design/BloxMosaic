using System.Reflection;
using MultiBloxy.Core;

namespace MultiBloxy;

internal sealed class TrayApplicationContext : ApplicationContext
{
    private readonly SettingsStore _settingsStore;
    private readonly LocalizationService _localization;
    private readonly FileLogger _logger;
    private readonly RobloxGuardService _guard;
    private readonly RobloxProcessService _processService;
    private readonly WindowsHandleCloser _handleCloser;
    private readonly ResourceCache _resources;
    private readonly CancellationTokenSource _shutdown = new();
    private readonly ContextMenuStrip _contextMenu;
    private readonly NotifyIcon _notifyIcon;
    private readonly Dictionary<string, ToolStripMenuItem> _languageItems =
        new(StringComparer.OrdinalIgnoreCase);

    private readonly ToolStripMenuItem _versionItem;
    private readonly ToolStripMenuItem _statusItem;
    private readonly ToolStripMenuItem _pauseItem;
    private readonly ToolStripMenuItem _reloadItem;
    private readonly ToolStripMenuItem _startItem;
    private readonly ToolStripMenuItem _stopAllItem;
    private readonly ToolStripMenuItem _settingsItem;
    private readonly ToolStripMenuItem _pauseOnLaunchItem;
    private readonly ToolStripMenuItem _resetRememberedItem;
    private readonly ToolStripMenuItem _languageItem;

    private AppSettings _settings;
    private GuardState _state = GuardState.Paused;
    private GuardState _stateBeforeBusy = GuardState.Paused;
    private bool _operationInProgress;
    private bool _firstIdleHandled;
    private bool _disposed;

    public TrayApplicationContext(
        SettingsStore settingsStore,
        SettingsLoadResult loadResult,
        LocalizationService localization,
        FileLogger logger)
    {
        ArgumentNullException.ThrowIfNull(settingsStore);
        ArgumentNullException.ThrowIfNull(loadResult);
        ArgumentNullException.ThrowIfNull(localization);
        ArgumentNullException.ThrowIfNull(logger);

        _settingsStore = settingsStore;
        _settings = loadResult.Settings.Normalize();
        _localization = localization;
        _logger = logger;
        _guard = new RobloxGuardService(AppConstants.RobloxGuardName);
        _processService = new RobloxProcessService(AppConstants.RobloxProcessName);
        _handleCloser = new WindowsHandleCloser(
            AppConstants.RobloxProcessName,
            AppConstants.RobloxGuardName);
        _resources = new ResourceCache();

        _contextMenu = new ContextMenuStrip();
        _versionItem = CreateMenuItem(imageName: "info");
        _versionItem.Click += (_, _) => OpenWebAddress(AppConstants.Homepage);
        _contextMenu.Items.Add(_versionItem);
        _contextMenu.Items.Add(new ToolStripSeparator());

        _statusItem = CreateMenuItem(imageName: "info");
        _statusItem.Enabled = false;
        _contextMenu.Items.Add(_statusItem);
        _contextMenu.Items.Add(new ToolStripSeparator());

        _pauseItem = CreateMenuItem(TextKeys.MenuPause, "pause");
        _pauseItem.Click += async (_, _) =>
            await ExecuteOperationAsync(ToggleGuardAsync).ConfigureAwait(true);
        _contextMenu.Items.Add(_pauseItem);

        _reloadItem = CreateMenuItem(TextKeys.MenuReload, "refresh-ccw");
        _reloadItem.Click += async (_, _) =>
            await ExecuteOperationAsync(ReloadGuardAsync).ConfigureAwait(true);
        _contextMenu.Items.Add(_reloadItem);
        _contextMenu.Items.Add(new ToolStripSeparator());

        _startItem = CreateMenuItem(TextKeys.MenuStart, "plus");
        _startItem.Click += (_, _) => StartRoblox();
        _contextMenu.Items.Add(_startItem);

        _stopAllItem = CreateMenuItem(TextKeys.MenuStopAll, "minus");
        _stopAllItem.Click += async (_, _) =>
            await ExecuteOperationAsync(StopAllFromMenuAsync).ConfigureAwait(true);
        _contextMenu.Items.Add(_stopAllItem);
        _contextMenu.Items.Add(new ToolStripSeparator());

        ToolStripMenuItem showInExplorer =
            CreateMenuItem(TextKeys.MenuShowInExplorer, "folder");
        showInExplorer.Click += (_, _) => ShowApplicationInExplorer();
        _contextMenu.Items.Add(showInExplorer);

        ToolStripMenuItem openLogs = CreateMenuItem(TextKeys.MenuOpenLogs, "folder");
        openLogs.Click += (_, _) => OpenLogsFolder();
        _contextMenu.Items.Add(openLogs);
        _contextMenu.Items.Add(new ToolStripSeparator());

        _settingsItem = CreateMenuItem(TextKeys.MenuSettings, "settings");
        _contextMenu.Items.Add(_settingsItem);

        _pauseOnLaunchItem = CreateMenuItem(TextKeys.MenuPauseOnLaunch, "pause");
        _pauseOnLaunchItem.CheckOnClick = true;
        _pauseOnLaunchItem.Checked = _settings.PauseOnLaunch;
        _pauseOnLaunchItem.Click += (_, _) => UpdatePauseOnLaunch();
        _settingsItem.DropDownItems.Add(_pauseOnLaunchItem);

        _resetRememberedItem = CreateMenuItem(TextKeys.MenuResetRemembered, "list-restart");
        _resetRememberedItem.Click += (_, _) => ResetRememberedAction();
        _settingsItem.DropDownItems.Add(_resetRememberedItem);

        _languageItem = CreateMenuItem(TextKeys.MenuLanguage, "globe");
        _settingsItem.DropDownItems.Add(_languageItem);
        BuildLanguageMenu();

        _contextMenu.Items.Add(new ToolStripSeparator());
        ToolStripMenuItem exitItem = CreateMenuItem(TextKeys.MenuExit, "x");
        exitItem.Click += (_, _) => ExitThread();
        _contextMenu.Items.Add(exitItem);

        _notifyIcon = new NotifyIcon
        {
            ContextMenuStrip = _contextMenu,
            Icon = _resources.GetIcon("icon-disabled"),
            Text = AppConstants.Name,
            Visible = true,
        };
        _notifyIcon.MouseClick += async (_, eventArgs) =>
        {
            if (eventArgs.Button == MouseButtons.Left)
            {
                await ExecuteOperationAsync(ToggleGuardAsync).ConfigureAwait(true);
            }
        };

        ApplyLocalization();
        SetState(GuardState.Paused);
        Application.Idle += OnFirstIdle;

        _logger.Info(
            $"MultiBloxy {Assembly.GetExecutingAssembly().GetName().Version} started. "
            + $"Settings: {_settingsStore.SettingsPath}");
        if (loadResult.Migrated)
        {
            _logger.Info("Legacy settings were migrated to the per-user data directory.");
        }

        if (loadResult.Warning is not null)
        {
            _logger.Warning(loadResult.Warning);
            ShowBalloon(
                _localization.Get(TextKeys.ErrorOperationCaption),
                _localization.Format(TextKeys.ResultSettingsWarning, loadResult.Warning),
                ToolTipIcon.Warning);
        }
    }

    protected override void ExitThreadCore()
    {
        if (_disposed)
        {
            base.ExitThreadCore();
            return;
        }

        _disposed = true;
        Application.Idle -= OnFirstIdle;
        _shutdown.Cancel();
        _guard.Disable();
        _notifyIcon.Visible = false;
        _notifyIcon.Dispose();
        _contextMenu.Dispose();
        _resources.Dispose();
        _guard.Dispose();
        _shutdown.Dispose();
        _logger.Info("MultiBloxy exited normally.");
        base.ExitThreadCore();
    }

    private ToolStripMenuItem CreateMenuItem(
        string? textKey = null,
        string? imageName = null)
    {
        return new ToolStripMenuItem
        {
            Tag = textKey,
            Name = textKey ?? string.Empty,
            Image = imageName is null ? null : _resources.GetImage(imageName),
        };
    }

    private void BuildLanguageMenu()
    {
        ToolStripMenuItem autoDetect = new()
        {
            Tag = TextKeys.MenuAutoDetect,
            CheckOnClick = false,
        };
        autoDetect.Click += (_, _) => SelectLanguage(locale: null);
        _languageItem.DropDownItems.Add(autoDetect);
        _languageItem.DropDownItems.Add(new ToolStripSeparator());

        foreach (string locale in _localization.SupportedLocales.OrderBy(value => value, StringComparer.Ordinal))
        {
            ToolStripMenuItem localeItem = new()
            {
                Text = _localization.GetLocaleDisplayName(locale),
                CheckOnClick = false,
            };
            localeItem.Click += (_, _) => SelectLanguage(locale);
            _languageItems.Add(locale, localeItem);
            _languageItem.DropDownItems.Add(localeItem);
        }
    }

    private async void OnFirstIdle(object? sender, EventArgs eventArgs)
    {
        if (_firstIdleHandled)
        {
            return;
        }

        _firstIdleHandled = true;
        Application.Idle -= OnFirstIdle;
        if (!_settings.PauseOnLaunch)
        {
            await ExecuteOperationAsync(
                () => EnableGuardWithRecoveryAsync(allowRememberedChoice: true)).ConfigureAwait(true);
        }
    }

    private async Task ToggleGuardAsync()
    {
        if (_guard.IsEnabled)
        {
            _guard.Disable();
            _logger.Info("The Roblox guard was paused by the user.");
            SetState(GuardState.Paused);
            return;
        }

        await EnableGuardWithRecoveryAsync(allowRememberedChoice: false).ConfigureAwait(true);
    }

    private async Task ReloadGuardAsync()
    {
        _guard.Disable();
        await EnableGuardWithRecoveryAsync(allowRememberedChoice: false).ConfigureAwait(true);
    }

    private async Task EnableGuardWithRecoveryAsync(bool allowRememberedChoice)
    {
        GuardAttemptResult initialAttempt = _guard.TryEnable();
        if (initialAttempt.Success)
        {
            _logger.Info("The Roblox guard was enabled.");
            SetState(GuardState.Running);
            return;
        }

        _logger.Warning($"The Roblox guard could not be enabled: {initialAttempt.Error}");
        if (allowRememberedChoice
            && _settings.RememberedAction == MutexRecoveryAction.Ignore)
        {
            SetState(GuardState.Error);
            ShowBalloon(
                _localization.Get(TextKeys.ErrorMutexCaption),
                _localization.Get(TextKeys.ResultFinalFailure),
                ToolTipIcon.Warning);
            return;
        }

        using RecoveryDialog dialog = new(_localization);
        if (dialog.ShowDialog() != DialogResult.OK)
        {
            SetState(GuardState.Error);
            return;
        }

        MutexRecoveryAction action = dialog.SelectedAction;
        _settings = _settings with
        {
            RememberedAction = dialog.RememberChoice
                ? action
                : MutexRecoveryAction.None,
        };
        SaveSettings();

        switch (action)
        {
            case MutexRecoveryAction.FixHandles:
                await CloseMatchingHandlesAsync().ConfigureAwait(true);
                break;

            case MutexRecoveryAction.StopAllProcesses:
                if (!await ConfirmAndStopAllAsync().ConfigureAwait(true))
                {
                    SetState(GuardState.Error);
                    return;
                }

                break;

            case MutexRecoveryAction.Ignore:
                SetState(GuardState.Error);
                return;

            case MutexRecoveryAction.Retry:
                break;

            default:
                SetState(GuardState.Error);
                return;
        }

        // Exactly one recovery attempt is allowed. A second failure is reported
        // without reopening this dialog or invoking another destructive action.
        GuardAttemptResult finalAttempt = _guard.TryEnable();
        if (finalAttempt.Success)
        {
            _logger.Info("The Roblox guard was enabled after one recovery action.");
            SetState(GuardState.Running);
            return;
        }

        _logger.Warning($"The recovery attempt failed: {finalAttempt.Error}");
        SetState(GuardState.Error);
        MessageBox.Show(
            _localization.Get(TextKeys.ResultFinalFailure),
            _localization.Get(TextKeys.ErrorMutexCaption),
            MessageBoxButtons.OK,
            MessageBoxIcon.Warning);
    }

    private async Task CloseMatchingHandlesAsync()
    {
        HandleCloseResult result = await _handleCloser.CloseMatchingHandlesAsync(
            _shutdown.Token).ConfigureAwait(true);
        string summary = _localization.Format(
            TextKeys.ResultFix,
            result.ScannedHandles,
            result.ProcessCount,
            result.ClosedHandles,
            result.FailedInspections);

        if (result.Error is not null)
        {
            _logger.Warning($"Handle recovery was not completed: {result.Error}");
            ShowError(result.Error);
            return;
        }

        _logger.Info(summary);
        ShowBalloon(AppConstants.Name, summary, ToolTipIcon.Info);
    }

    private async Task StopAllFromMenuAsync()
    {
        await ConfirmAndStopAllAsync().ConfigureAwait(true);
    }

    private async Task<bool> ConfirmAndStopAllAsync()
    {
        int processCount = _processService.GetProcessCount();
        if (processCount == 0)
        {
            ShowBalloon(
                AppConstants.Name,
                _localization.Get(TextKeys.ResultNoProcesses),
                ToolTipIcon.Info);
            return true;
        }

        DialogResult confirmation = MessageBox.Show(
            _localization.Format(TextKeys.ConfirmStopMessage, processCount),
            _localization.Get(TextKeys.ConfirmStopCaption),
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Warning,
            MessageBoxDefaultButton.Button2);
        if (confirmation != DialogResult.Yes)
        {
            return false;
        }

        ProcessStopResult result = await _processService.StopAllAsync(
            _shutdown.Token).ConfigureAwait(true);
        string summary = _localization.Format(
            TextKeys.ResultStop,
            result.Stopped,
            result.AlreadyExited,
            result.Failed);
        _logger.Info(summary);
        ShowBalloon(
            AppConstants.Name,
            summary,
            result.Failed == 0 ? ToolTipIcon.Info : ToolTipIcon.Warning);
        return !result.Cancelled;
    }

    private void StartRoblox()
    {
        ProcessLaunchResult result = _processService.StartRoblox();
        if (!result.Success)
        {
            _logger.Warning($"The Roblox URI handler could not be started: {result.Error}");
            ShowError(result.Error ?? "The Roblox URI handler could not be started.");
        }
    }

    private void OpenWebAddress(string address)
    {
        ProcessLaunchResult result = _processService.OpenWebAddress(address);
        if (!result.Success)
        {
            _logger.Warning($"A web address could not be opened: {result.Error}");
            ShowError(result.Error ?? "The web address could not be opened.");
        }
    }

    private void ShowApplicationInExplorer()
    {
        ProcessLaunchResult result = _processService.OpenInExplorer(Application.ExecutablePath);
        if (!result.Success)
        {
            _logger.Warning($"Explorer could not show the application: {result.Error}");
            ShowError(result.Error ?? "Windows Explorer could not show the application.");
        }
    }

    private void OpenLogsFolder()
    {
        ProcessLaunchResult result = _processService.OpenFolder(_logger.LogDirectory);
        if (!result.Success)
        {
            _logger.Warning($"The log folder could not be opened: {result.Error}");
            ShowError(result.Error ?? "The log folder could not be opened.");
        }
    }

    private void UpdatePauseOnLaunch()
    {
        _settings = _settings with { PauseOnLaunch = _pauseOnLaunchItem.Checked };
        SaveSettings();
    }

    private void ResetRememberedAction()
    {
        _settings = _settings with { RememberedAction = MutexRecoveryAction.None };
        SaveSettings();
    }

    private void SelectLanguage(string? locale)
    {
        _settings = _settings with { Language = locale };
        _localization.SetLocale(locale);
        SaveSettings();
        ApplyLocalization();
        UpdateUi();
    }

    private void SaveSettings()
    {
        SettingsSaveResult saveResult = _settingsStore.Save(_settings);
        if (saveResult.Success)
        {
            return;
        }

        string error = saveResult.Error ?? "Unknown settings error.";
        _logger.Warning($"Settings could not be saved: {error}");
        ShowBalloon(
            _localization.Get(TextKeys.ErrorOperationCaption),
            _localization.Format(TextKeys.ResultSettingsWarning, error),
            ToolTipIcon.Warning);
    }

    private async Task ExecuteOperationAsync(Func<Task> operation)
    {
        if (_operationInProgress || _disposed)
        {
            return;
        }

        _operationInProgress = true;
        _stateBeforeBusy = _state;
        SetState(GuardState.Busy);

        try
        {
            await operation().ConfigureAwait(true);
        }
        catch (OperationCanceledException) when (_shutdown.IsCancellationRequested)
        {
            // Normal during application shutdown.
        }
        catch (Exception exception)
        {
            _logger.Error("A tray operation failed.", exception);
            SetState(GuardState.Error);
            ShowError(exception.Message);
        }
        finally
        {
            _operationInProgress = false;
            if (!_disposed && _state == GuardState.Busy)
            {
                SetState(_stateBeforeBusy);
            }
            else if (!_disposed)
            {
                UpdateUi();
            }
        }
    }

    private void SetState(GuardState state)
    {
        _state = state;
        if (!_disposed)
        {
            UpdateUi();
        }
    }

    private void UpdateUi()
    {
        string statusKey = _state switch
        {
            GuardState.Running => TextKeys.StatusRunning,
            GuardState.Busy => TextKeys.StatusBusy,
            GuardState.Error => TextKeys.StatusError,
            _ => TextKeys.StatusPaused,
        };

        _statusItem.Tag = statusKey;
        _pauseItem.Tag = _guard.IsEnabled ? TextKeys.MenuPause : TextKeys.MenuResume;
        _pauseItem.Image = _resources.GetImage(_guard.IsEnabled ? "pause" : "play");
        _notifyIcon.Icon = _resources.GetIcon(
            _state == GuardState.Running ? "icon" : "icon-disabled");

        bool enabled = !_operationInProgress;
        _pauseItem.Enabled = enabled;
        _reloadItem.Enabled = enabled;
        _startItem.Enabled = enabled;
        _stopAllItem.Enabled = enabled;
        _settingsItem.Enabled = enabled;

        ApplyLocalization();
        string status = _localization.Get(statusKey);
        _notifyIcon.Text = $"{AppConstants.Name} — {status}";
    }

    private void ApplyLocalization()
    {
        ApplyLocalization(_contextMenu.Items);

        Version? version = Assembly.GetExecutingAssembly().GetName().Version;
        _versionItem.Text = $"{AppConstants.Name} {version?.ToString(3) ?? "2.0.0"}";
        _pauseOnLaunchItem.Checked = _settings.PauseOnLaunch;
        _resetRememberedItem.Enabled =
            !_operationInProgress && _settings.RememberedAction != MutexRecoveryAction.None;

        ToolStripMenuItem autoDetect = (ToolStripMenuItem)_languageItem.DropDownItems[0];
        autoDetect.Checked = _settings.Language is null;
        foreach ((string locale, ToolStripMenuItem item) in _languageItems)
        {
            item.Checked = string.Equals(_settings.Language, locale, StringComparison.OrdinalIgnoreCase);
            item.Text = _localization.GetLocaleDisplayName(locale);
        }
    }

    private void ApplyLocalization(ToolStripItemCollection items)
    {
        foreach (ToolStripItem item in items)
        {
            if (item.Tag is string key && !string.IsNullOrWhiteSpace(key))
            {
                item.Text = _localization.Get(key);
            }

            if (item is ToolStripMenuItem menuItem && menuItem.HasDropDownItems)
            {
                ApplyLocalization(menuItem.DropDownItems);
            }
        }
    }

    private void ShowError(string message)
    {
        MessageBox.Show(
            message,
            _localization.Get(TextKeys.ErrorOperationCaption),
            MessageBoxButtons.OK,
            MessageBoxIcon.Error);
    }

    private void ShowBalloon(string title, string message, ToolTipIcon icon)
    {
        if (_disposed)
        {
            return;
        }

        _notifyIcon.ShowBalloonTip(
            timeout: 4_000,
            tipTitle: title,
            tipText: message,
            tipIcon: icon);
    }
}
