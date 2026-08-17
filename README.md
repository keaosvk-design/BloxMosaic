# MultiBloxy

MultiBloxy is a small 64-bit Windows tray application that coordinates the
`ROBLOX_singletonEvent` named object used by the Roblox client. Its goal is to
allow multiple unmodified Roblox client processes to coexist and to provide
simple pause, retry, launch, and process-management controls.

> [!IMPORTANT]
> Roblox actively changes its client and may block multi-instance tools.
> MultiBloxy is not guaranteed to work with the current client. The project also
> cannot guarantee that every use is acceptable under current Roblox rules. Read
> the [security and compatibility notice](SECURITY.md) before running it.

## Project status

The `2.x` codebase is a modernization of the original .NET Framework utility:

- .NET 10 LTS and Windows Forms;
- explicit Windows x64 target;
- bounded, x64-correct native handle inspection;
- no recursive retries or remembered destructive actions;
- per-user atomic settings and local diagnostic logs;
- core unit tests and Windows integration tests;
- automated Windows build and release workflows.

Runtime compatibility with Roblox still requires a manual test on Windows. CI
can verify MultiBloxy itself, but cannot safely automate a live Roblox session.

## Features

- Lightweight Windows notification-area application.
- Pause, resume, and reload the Roblox guard.
- Start Roblox through the registered `roblox-player:` handler.
- Request a normal close of all Roblox processes, with confirmation and a
  bounded forced-close fallback.
- Optional advanced recovery that closes only an exactly matching named handle.
- English and Russian interface.
- Settings migration from the legacy portable `config.xml`.
- No account storage, HTTP client, telemetry, updater, or administrator requirement.

## Requirements

To run the application:

- a supported 64-bit Windows version;
- an installed, unmodified Roblox client;
- standard user permissions.

MultiBloxy is not a Linux or macOS application. CachyOS can build and publish the
Windows executable, but cannot run or validate its Windows/Roblox integration.

## Download and run

1. Open the repository's **Releases** page.
2. Download `MultiBloxy.exe` and `MultiBloxy.exe.sha256`, or the
   `MultiBloxy-<version>-win-x64.zip` archive.
3. Verify the SHA-256 value as described in [Getting Started](GETTING_STARTED.md).
4. Run `MultiBloxy.exe` as a normal user.
5. Use the MultiBloxy icon in the Windows notification area.

Do not disable security software or automatically bypass a SmartScreen warning.
An unsigned build has no verified publisher. Prefer a release produced by the
repository workflow, compare its checksum, and compile from source when unsure.

## Configuration and logs

MultiBloxy writes only per-user local files:

```text
Settings: %LOCALAPPDATA%\MultiBloxy\config.xml
Logs:     %LOCALAPPDATA%\MultiBloxy\logs\MultiBloxy.log
```

On first launch, a valid legacy `config.xml` next to the executable is copied to
the new settings location. The legacy file is not deleted. A malformed current
config is quarantined as `config.corrupt-<timestamp>-<id>.xml`, and safe defaults
are used.

The program does not store Roblox usernames, passwords, cookies, or tokens.

## Build on CachyOS / Arch Linux

Update the system and install the tools:

```bash
sudo pacman -Syu
sudo pacman -S --needed git dotnet-sdk-10.0
```

Clone and build from the repository root:

```bash
git clone https://github.com/Zgoly/MultiBloxy.git
cd MultiBloxy
dotnet restore MultiBloxy.sln
dotnet build MultiBloxy.sln --configuration Release --no-restore
```

Run the cross-platform core tests:

```bash
dotnet test tests/MultiBloxy.Core.Tests/MultiBloxy.Core.Tests.csproj --configuration Release
```

Create a self-contained Windows executable from CachyOS:

```bash
bash scripts/publish-windows.sh
```

Result:

```text
dist/windows-x64/MultiBloxy.exe
dist/windows-x64/MultiBloxy.exe.sha256
dist/windows-x64/release-manifest.json
```

The cross-published executable must still be tested on Windows. See the complete
[build guide](BUILDING.md).

## Build and run on Windows

From PowerShell in the repository root:

```powershell
dotnet restore MultiBloxy.sln
dotnet build MultiBloxy.sln --configuration Debug --no-restore
dotnet test MultiBloxy.sln --configuration Debug --no-build
dotnet run --project MultiBloxy/MultiBloxy.csproj --configuration Debug
```

For a portable release:

```powershell
dotnet publish MultiBloxy/MultiBloxy.csproj `
  --configuration Release `
  --runtime win-x64 `
  --self-contained true
```

The default publish result is under:

```text
MultiBloxy\bin\Release\net10.0-windows\win-x64\publish\MultiBloxy.exe
```

## Tests

The solution contains two test projects:

- `MultiBloxy.Core.Tests` runs on Linux and Windows and tests settings,
  migration, unsafe XML rejection, recovery policy, and localization.
- `MultiBloxy.Windows.Tests` runs only on 64-bit Windows and checks the native
  layout, exact name matching, and a GUID-named test Event inside the test process.

Commands:

```bash
dotnet test tests/MultiBloxy.Core.Tests/MultiBloxy.Core.Tests.csproj -c Release
```

```powershell
dotnet test MultiBloxy.sln -c Release
```

See [the test matrix](docs/TEST-MATRIX.md) for the manual Roblox checks that CI
cannot perform.

## Repository structure

```text
MultiBloxy.Core/              platform-neutral settings and localization
MultiBloxy/                   Windows Forms application and Windows services
tests/MultiBloxy.Core.Tests/  Linux/Windows unit tests
tests/MultiBloxy.Windows.Tests/ Windows native integration tests
scripts/                      reproducible build/test/publish commands
.github/workflows/            CI, CodeQL, and Windows release automation
docs/                         baseline, architecture, and release notes
```

## Windows `.exe` and releases

The preferred release method is `.github/workflows/release.yml` on a real GitHub
Windows runner. A tag such as `v2.0.0` builds and tests the solution, publishes a
self-contained single-file `MultiBloxy.exe`, computes SHA-256, records the source
commit in `release-manifest.json`, creates a ZIP, and attaches the artifacts to a
GitHub Release.

The executable is portable. It is not a `Setup.exe` installer: it does not write
registry installation records, add an uninstaller, or configure startup.

## Development

- Read [BUILDING.md](BUILDING.md) for setup and release commands.
- Read [CONTRIBUTING.md](CONTRIBUTING.md) before changing native code.
- Read [TROUBLESHOOTING.md](TROUBLESHOOTING.md) when a command fails.
- Read [SECURITY.md](SECURITY.md) for the threat model and reporting process.
- Architecture is documented in [docs/ARCHITECTURE.md](docs/ARCHITECTURE.md).

Changes that add client modification, code injection, a kernel driver, privilege
escalation, anti-cheat evasion, credential handling, or hidden destructive behavior
are outside this project's scope.

## License

MultiBloxy is licensed under the [MIT License](LICENSE).
