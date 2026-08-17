# Contributing

## Development setup

Use .NET SDK 10.0.400 or a newer stable .NET 10 feature band.

```bash
git clone https://github.com/Zgoly/MultiBloxy.git
cd MultiBloxy
git switch -c feature/short-description
dotnet restore MultiBloxy.sln
dotnet build MultiBloxy.sln -c Release --no-restore
dotnet test tests/MultiBloxy.Core.Tests/MultiBloxy.Core.Tests.csproj -c Release --no-build
```

Run the full solution tests on Windows before changing Windows integration code:

```powershell
dotnet test MultiBloxy.sln -c Release
```

## Pull requests

- Keep one logical change per commit.
- Add a regression test for every fixed serious bug when practical.
- Preserve migration from the legacy XML config.
- Keep user-visible English and Russian keys in sync.
- Run `dotnet format MultiBloxy.sln --verify-no-changes --severity warn`.
- Do not commit `bin`, `obj`, `dist`, logs, credentials, certificates, or Roblox
  account data.

## Native-code rules

Changes to `WindowsHandleCloser.cs` require:

- an explicit x64 layout explanation;
- bounds and NTSTATUS validation;
- `SafeHandle` or guaranteed `finally` cleanup;
- no `PROCESS_ALL_ACCESS`;
- exact object-name matching;
- a Windows integration test using a unique test object, never a live Roblox
  session in CI;
- a documented fail-closed path.

Pull requests that add injection, a driver, elevation, anti-cheat evasion, or
silent process termination are outside project scope.
