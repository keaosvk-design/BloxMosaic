# Building MultiBloxy

MultiBloxy is a C#/.NET 10 Windows Forms application. CachyOS can compile and
cross-publish it for Windows, but only Windows can run the app and execute the
native integration tests.

## Debug versus Release

- **Debug** keeps development symbols and disables most optimizations. Use it
  while changing code or investigating an error.
- **Release** enables optimizations and is used for distributable artifacts.

A Release build is not automatically trusted or tested; CI results and release
provenance provide that evidence.

## Build from a clean CachyOS / Arch Linux system

### 1. Install tools

Open a terminal in any directory:

```bash
sudo pacman -Syu
sudo pacman -S --needed git dotnet-sdk-10.0
```

`pacman -Syu` performs the required full rolling-release update. If it upgrades
the kernel or core libraries, reboot before diagnosing unrelated tool failures.

Verify the tools:

```bash
git --version
dotnet --info
dotnet --list-sdks
```

The SDK list must contain .NET `10.0.100` or a newer stable .NET 10 feature band.

### 2. Clone

From the home directory:

```bash
mkdir -p ~/Projects
cd ~/Projects
git clone https://github.com/Zgoly/MultiBloxy.git
cd MultiBloxy
git status
```

For a fresh clone, `git status` should report a clean working tree.

### 3. Restore dependencies

From `~/Projects/MultiBloxy`:

```bash
dotnet restore MultiBloxy.sln
```

This downloads the Windows targeting pack and the MSTest packages. `NU1301`
usually means that NuGet is unreachable; do not add random package mirrors.

### 4. Build Debug

```bash
cd ~/Projects/MultiBloxy
dotnet build MultiBloxy.sln --configuration Debug --no-restore
```

The compilation output appears under:

```text
MultiBloxy/bin/Debug/net10.0-windows/
```

This ordinary build proves that the source compiles and contains the managed
`MultiBloxy.dll`; it is not the distributable Windows package. Do not try to run
the generated app host on CachyOS. Use step 7 to create the real Windows `.exe`.

### 5. Build Release

```bash
cd ~/Projects/MultiBloxy
dotnet build MultiBloxy.sln --configuration Release --no-restore
```

Compilation output:

```text
MultiBloxy/bin/Release/net10.0-windows/
```

This ordinary build is not the final self-contained portable package. The
publish step below explicitly selects the `win-x64` runtime so that the host is
a Windows executable even though the command runs on Linux.

### 6. Run tests on CachyOS

Only the platform-neutral tests can execute on Linux:

```bash
cd ~/Projects/MultiBloxy
dotnet test tests/MultiBloxy.Core.Tests/MultiBloxy.Core.Tests.csproj --configuration Release
```

Expected successful summary resembles `Failed: 0`. The exact test count can grow
over time. `MultiBloxy.Windows.Tests` must be run on Windows.

### 7. Cross-publish Windows x64

```bash
cd ~/Projects/MultiBloxy
bash scripts/publish-windows.sh
```

Output:

```text
dist/windows-x64/MultiBloxy.exe
dist/windows-x64/MultiBloxy.exe.sha256
dist/windows-x64/release-manifest.json
dist/windows-x64/LICENSE.txt
dist/windows-x64/README.txt
```

To override the package version recorded in the executable and manifest:

```bash
MULTIBLOXY_VERSION=2.0.1 bash scripts/publish-windows.sh
```

The `.exe` is self-contained and single-file. It includes the .NET runtime but
still runs only on Windows x64. Copy the output to Windows and test it there.
When publishing from a source archive without Git metadata, the manifest records
`gitCommit` as `unknown`; a normal clone records the exact source commit.

## Build and run on Windows

Install the .NET 10 SDK. On a Windows machine with `winget`:

```powershell
winget install Microsoft.DotNet.SDK.10
```

Open PowerShell in the repository directory:

```powershell
dotnet restore MultiBloxy.sln
dotnet build MultiBloxy.sln --configuration Debug --no-restore
dotnet test MultiBloxy.sln --configuration Debug --no-build
dotnet run --project MultiBloxy/MultiBloxy.csproj --configuration Debug
```

Exit the tray application before starting another development run.

Publish a portable executable locally on Windows:

```powershell
dotnet publish MultiBloxy/MultiBloxy.csproj `
  --configuration Release `
  --runtime win-x64 `
  --self-contained true `
  -p:PublishSingleFile=true `
  -p:IncludeNativeLibrariesForSelfExtract=true `
  -p:PublishTrimmed=false `
  -p:DebugType=None `
  -p:DebugSymbols=false
```

Result:

```text
MultiBloxy\bin\Release\net10.0-windows\win-x64\publish\MultiBloxy.exe
```

## Preferred `.exe` method: GitHub Actions

The Windows runner is preferred because it compiles and tests on the actual
operating-system family targeted by the application.

### Upload the modernization branch

If you own the target repository, from `~/Projects/MultiBloxy`:

```bash
git status
git add .
git commit -m "Modernize MultiBloxy for .NET 10"
git push -u origin global-modernization
```

If `origin` is the upstream repository and you do not have write access, create a
fork on GitHub and replace `<YOUR-USER>` below:

```bash
git remote rename origin upstream
git remote add origin https://github.com/<YOUR-USER>/MultiBloxy.git
git push -u origin global-modernization
```

Do not paste the angle brackets literally. Use your GitHub username.

### Download an Actions artifact

1. Open the repository on GitHub.
2. Open **Actions**.
3. Select **CI** or **Windows release**.
4. Open the newest successful run.
5. At the bottom of the run, open **Artifacts**.
6. Download `MultiBloxy-win-x64` from a release-workflow run.
7. Extract it and verify `MultiBloxy.exe.sha256`.

### Create a release

After CI is green and the intended commit is checked out:

```bash
cd ~/Projects/MultiBloxy
git status
git tag -a v2.0.0 -m "MultiBloxy 2.0.0"
git push origin v2.0.0
```

The tag starts `.github/workflows/release.yml`. The workflow builds, runs both
test projects on Windows, publishes the self-contained executable, computes its
hash, creates a ZIP, and creates the GitHub Release automatically.

On GitHub, open **Releases** on the repository page. The release contains:

```text
MultiBloxy.exe
MultiBloxy.exe.sha256
release-manifest.json
MultiBloxy-2.0.0-win-x64.zip
```

If release creation reports a permission error, a repository owner must open
**Settings → Actions → General → Workflow permissions** and permit the workflow
to write repository contents.

## Portable executable versus installer

`MultiBloxy.exe` is a portable application. It can be copied to a stable folder
and launched directly.

A `Setup.exe` installer would additionally register installation state, create
shortcuts, and provide an uninstaller. This repository intentionally does not add
an installer yet: a verified portable executable is simpler and introduces less
release and privilege surface.
