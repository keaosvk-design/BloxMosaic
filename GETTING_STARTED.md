# Getting started with MultiBloxy

## Before you run it

MultiBloxy is a portable Windows x64 application. It changes how the Roblox
client coordinates multiple processes. Roblox may block this behavior, and the
project cannot promise that it works with every current client version or that
every use complies with Roblox rules.

MultiBloxy does not need administrator rights. Do not run it as administrator as
a workaround for a normal error.

## Download and verify a release

Download these files from the same GitHub Release:

```text
MultiBloxy.exe
MultiBloxy.exe.sha256
```

Open PowerShell in the download directory and run:

```powershell
Get-FileHash .\MultiBloxy.exe -Algorithm SHA256
Get-Content .\MultiBloxy.exe.sha256
```

The long hexadecimal values must be identical, ignoring letter case. If they are
different, delete the downloaded file and do not run it.

An unsigned executable can trigger SmartScreen because Windows cannot identify a
verified publisher. A checksum proves that the file matches the release artifact;
it does not replace code signing or guarantee that a program is harmless. Build
from source when you cannot establish trust.

## First launch

1. Double-click `MultiBloxy.exe` on Windows.
2. Look for its icon in the notification area near the clock. Windows may place
   it in the hidden-icons menu.
3. Right-click the icon to open the menu.
4. Confirm that the status is either `Running`, `Paused`, or `Error`.

`Running` means that MultiBloxy owns the named guard object. It does not guarantee
that the current Roblox version permits multiple live sessions.

## Tray controls

- **Pause / Resume** releases or recreates the guard.
- **Reload guard** performs one controlled disable/enable cycle.
- **Start new Roblox instance** opens the registered `roblox-player:` URI.
- **Stop all Roblox instances…** always asks for confirmation. It requests a
  normal close first and forces termination only for processes that remain open.
- **Open logs folder** opens local diagnostic logs.
- **Pause on launch** starts MultiBloxy without enabling its guard.
- **Reset remembered choice** clears the only rememberable recovery choice.

When guard creation fails, MultiBloxy offers exactly one recovery action. It does
not recursively reopen the dialog. Handle repair and process termination are
never stored for automatic execution; only continuing with the guard disabled can
be remembered.

## Start MultiBloxy with Windows

Use the current user's Startup folder rather than the all-users system folder:

1. Keep `MultiBloxy.exe` in a stable directory, for example
   `%LOCALAPPDATA%\Programs\MultiBloxy`.
2. Press <kbd>Win</kbd>+<kbd>R</kbd>.
3. Enter `shell:startup` and press Enter.
4. Right-drag `MultiBloxy.exe` into the folder and choose **Create shortcuts here**.

Create a shortcut, not a second copy of the executable. MultiBloxy does not modify
Windows startup automatically.

## Configuration, logs, and reset

```text
%LOCALAPPDATA%\MultiBloxy\config.xml
%LOCALAPPDATA%\MultiBloxy\logs\MultiBloxy.log
```

If config is malformed, MultiBloxy moves it aside and starts with defaults.

To reset all local data:

1. Exit MultiBloxy from its tray menu.
2. Open `%LOCALAPPDATA%` in Explorer.
3. Delete only the `MultiBloxy` folder if you no longer need its settings or logs.

Deleting that folder permanently removes the local settings and diagnostic logs.
It does not uninstall Roblox or delete Roblox accounts.

## If multiple instances still do not work

1. Update Roblox through its official installer.
2. Exit MultiBloxy and all Roblox processes normally.
3. Start MultiBloxy as a standard user before Roblox.
4. Check `%LOCALAPPDATA%\MultiBloxy\logs\MultiBloxy.log`.
5. Review [TROUBLESHOOTING.md](TROUBLESHOOTING.md).

Do not install unofficial Roblox builds, disable anti-cheat, inject code, or use a
kernel driver. If the official client intentionally blocks the mechanism, there
may be no safe project-side fix.
