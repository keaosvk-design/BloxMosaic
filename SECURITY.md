# Security policy

## Supported versions

| Version | Security support |
|---|---|
| 2.x | Supported |
| 1.x (.NET Framework) | Unsupported; retained only in Git history/releases |

## Reporting a vulnerability

Use GitHub's private **Security → Advisories → New draft security advisory** flow
when available. Do not publish credentials, private account information, exploit
details, or sensitive logs in a public issue.

Include:

- affected commit/version;
- Windows version and architecture;
- minimal reproduction steps using a test process when possible;
- expected versus actual behavior;
- relevant log excerpt with usernames and paths redacted.

## Security boundaries

MultiBloxy:

- does not request administrator rights;
- does not modify Roblox files;
- does not inject code or load a driver;
- does not store Roblox credentials, cookies, or tokens;
- does not make HTTP requests or send telemetry;
- stores settings and logs under the current user's LocalAppData directory.

The advanced recovery feature uses Windows process-handle APIs and internal NT
handle-table information. It is restricted to 64-bit Windows, the
`RobloxPlayerBeta` process name, and the exact final object name
`ROBLOX_singletonEvent`. It requests only `PROCESS_DUP_HANDLE`, bounds all native
buffers, and fails closed when validation fails. The underlying enumeration API
is not a stable public contract, so this feature remains higher risk than ordinary
.NET code.

Changes that introduce client modification, code injection, privilege escalation,
kernel drivers, anti-cheat evasion, credential handling, or hidden destructive
behavior are not accepted.

## Release integrity

The release workflow publishes a self-contained executable and SHA-256 checksum
from a tagged commit after build and tests. SHA-256 detects accidental or malicious
file replacement but does not establish publisher identity. Code signing requires
a separately acquired certificate and should be added only through protected
GitHub secrets/environments; private keys must never be committed.

## Compatibility and account risk

Roblox can change or block its client behavior independently of this repository.
No contributor can guarantee uninterrupted multi-instance support or immunity
from account action. Users must review the current Roblox terms and make their own
decision before running the application.
