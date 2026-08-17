using MultiBloxy.Core;

namespace MultiBloxy;

internal sealed class RecoveryDialog : Form
{
    private readonly RadioButton _fixButton;
    private readonly RadioButton _stopAllButton;
    private readonly RadioButton _retryButton;
    private readonly RadioButton _ignoreButton;
    private readonly CheckBox _rememberCheckBox;

    public RecoveryDialog(LocalizationService localization)
    {
        ArgumentNullException.ThrowIfNull(localization);

        Text = localization.Get(TextKeys.ErrorMutexCaption);
        AutoSize = true;
        AutoSizeMode = AutoSizeMode.GrowAndShrink;
        AutoScaleMode = AutoScaleMode.Dpi;
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = true;
        Icon = SystemIcons.Warning;

        TableLayoutPanel layout = new()
        {
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            ColumnCount = 1,
            Dock = DockStyle.Fill,
            Padding = new Padding(12),
        };

        Label message = new()
        {
            AutoSize = true,
            MaximumSize = new Size(520, 0),
            Text = localization.Get(TextKeys.ErrorMutexMessage),
            Margin = new Padding(3, 3, 3, 12),
        };
        layout.Controls.Add(message);

        _fixButton = AddOption(layout, localization.Get(TextKeys.RecoveryFix));
        _stopAllButton = AddOption(layout, localization.Get(TextKeys.RecoveryStopAll));
        _retryButton = AddOption(layout, localization.Get(TextKeys.RecoveryRetry));
        _ignoreButton = AddOption(layout, localization.Get(TextKeys.RecoveryIgnore));
        _retryButton.Checked = true;

        _rememberCheckBox = new CheckBox
        {
            AutoSize = true,
            Enabled = false,
            Text = localization.Get(TextKeys.RecoveryRemember),
            Margin = new Padding(3, 12, 3, 3),
        };
        layout.Controls.Add(_rememberCheckBox);

        Label rememberHint = new()
        {
            AutoSize = true,
            ForeColor = SystemColors.GrayText,
            MaximumSize = new Size(520, 0),
            Text = localization.Get(TextKeys.RecoveryRememberHint),
            Margin = new Padding(24, 0, 3, 12),
        };
        layout.Controls.Add(rememberHint);

        FlowLayoutPanel buttons = new()
        {
            AutoSize = true,
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft,
            WrapContents = false,
        };

        Button confirm = new()
        {
            AutoSize = true,
            DialogResult = DialogResult.OK,
            Text = localization.Get(TextKeys.RecoveryConfirm),
        };
        Button cancel = new()
        {
            AutoSize = true,
            DialogResult = DialogResult.Cancel,
            Text = localization.Get(TextKeys.RecoveryCancel),
        };
        buttons.Controls.Add(confirm);
        buttons.Controls.Add(cancel);
        layout.Controls.Add(buttons);

        AcceptButton = confirm;
        CancelButton = cancel;
        Controls.Add(layout);

        _fixButton.CheckedChanged += (_, _) => UpdateRememberState();
        _stopAllButton.CheckedChanged += (_, _) => UpdateRememberState();
        _retryButton.CheckedChanged += (_, _) => UpdateRememberState();
        _ignoreButton.CheckedChanged += (_, _) => UpdateRememberState();
    }

    public MutexRecoveryAction SelectedAction =>
        _fixButton.Checked ? MutexRecoveryAction.FixHandles
        : _stopAllButton.Checked ? MutexRecoveryAction.StopAllProcesses
        : _ignoreButton.Checked ? MutexRecoveryAction.Ignore
        : MutexRecoveryAction.Retry;

    public bool RememberChoice =>
        _rememberCheckBox.Checked && RecoveryPolicy.CanRemember(SelectedAction);

    private static RadioButton AddOption(TableLayoutPanel layout, string text)
    {
        RadioButton option = new()
        {
            AutoSize = true,
            MaximumSize = new Size(520, 0),
            Text = text,
            Margin = new Padding(3, 4, 3, 4),
        };
        layout.Controls.Add(option);
        return option;
    }

    private void UpdateRememberState()
    {
        bool canRemember = RecoveryPolicy.CanRemember(SelectedAction);
        _rememberCheckBox.Enabled = canRemember;
        if (!canRemember)
        {
            _rememberCheckBox.Checked = false;
        }
    }
}
