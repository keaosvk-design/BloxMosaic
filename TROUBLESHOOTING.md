# Troubleshooting

## `dotnet: command not found` on CachyOS

From any directory:

```bash
sudo pacman -Syu
sudo pacman -S --needed dotnet-sdk-10.0
dotnet --info
```

Do not install an Ubuntu/Debian package or copy an unknown SDK binary into the
system.

## `A compatible .NET SDK was not found`

Check installed SDKs:

```bash
dotnet --list-sdks
```

This repository's `global.json` requires SDK 10.0.400 or a newer stable .NET 10
feature band. Update CachyOS fully if that SDK is missing.

## `NU1301` or NuGet restore failure

Verify that `https://api.nuget.org` is reachable and that the system date is
correct. Retry:

```bash
cd ~/Projects/MultiBloxy
dotnet restore MultiBloxy.sln
```

Do not add an untrusted NuGet source. List configured sources with:

```bash
dotnet nuget list source
```

## `NETSDK1100` when building on Linux

Run the command from the repository root and verify that the current
`MultiBloxy/MultiBloxy.csproj` contains `EnableWindowsTargeting`. Do not try to
convert the project to a Linux application; WinForms and the native feature are
Windows-only.

## Windows says the `.exe` cannot run on this PC

The release targets Windows x64. It does not run on Linux, macOS, Windows on ARM
without x64 emulation, or 32-bit Windows. Download the `win-x64` release and check
its SHA-256.

## SmartScreen shows an unknown publisher

The artifact is not code-signed until a trusted signing certificate is configured.
Do not automatically click **Run anyway**. Verify the checksum against the same
GitHub Release, inspect the source and successful workflow run, or build it
yourself. A future signed release should show a verified publisher.

## MultiBloxy says the guard cannot be enabled

Roblox may already own a named object with the same name, or the current Roblox
client may no longer support this mechanism.

1. Close Roblox normally.
2. Exit and restart MultiBloxy as a standard user.
3. Choose **Try once again**.
4. Use advanced handle recovery only when you understand that it changes a live
   process handle.
5. Open the logs from the tray menu.

MultiBloxy intentionally performs at most one recovery attempt. It never stores
handle repair or process termination for automatic execution.

## `Start new Roblox instance` does nothing

The official Roblox installer must register the `roblox-player:` URI handler.
Repair or reinstall Roblox from its official source. MultiBloxy does not download
or replace the client.

## The config is renamed to `config.corrupt-...xml`

MultiBloxy detected malformed or unsafe XML, quarantined the file, and loaded safe
defaults. Reconfigure the language and startup option through the tray menu. Do
not copy malformed XML back over the new config.

## No tray icon appears

Check the hidden icons near the Windows clock, then inspect:

```text
%LOCALAPPDATA%\MultiBloxy\logs\MultiBloxy.log
```

If another instance is running, MultiBloxy rejects the second launch for the same
Windows user.

## Windows integration test fails

Run it from 64-bit Windows as a normal user:

```powershell
dotnet test tests/MultiBloxy.Windows.Tests/MultiBloxy.Windows.Tests.csproj -c Release
```

Security software can deny `PROCESS_DUP_HANDLE`. Do not disable protection merely
to make the test pass; attach the TRX report and log to a GitHub issue.

## GitHub Release workflow cannot create a release

Verify:

- the tag follows `v<major>.<minor>.<patch>`, for example `v2.0.0`;
- CI passes on the tagged commit;
- workflow permissions allow `contents: write`;
- a release with the same tag does not already exist.
