# Changelog

All notable changes are documented here. The project follows Semantic Versioning
for the modernized `2.x` line.

## [Unreleased]

### Added

- .NET 10 SDK-style solution with a platform-neutral core project.
- Core unit tests and Windows x64 integration tests.
- GitHub Actions CI, CodeQL, Dependabot, and automated Windows release workflow.
- Per-user rotating diagnostic log.
- CachyOS build and Windows cross-publish scripts.

### Changed

- Explicit Windows x64 target replaces `AnyCPU`.
- Settings move to `%LOCALAPPDATA%\MultiBloxy` with automatic legacy migration.
- Native handle enumeration uses bounded x64 structures, one system snapshot,
  minimal process rights, and `SafeHandle` cleanup.
- Tray application uses a dedicated `ApplicationContext` and cached resources.
- Process termination requests a normal close first and requires confirmation.

### Fixed

- Removed recursive mutex-error handling and the resulting stack-overflow path.
- Destructive recovery actions can no longer be remembered or auto-executed.
- Corrupt/invalid XML no longer prevents application startup.
- Image streams and replaced tray icons no longer leak GDI resources.
- All long handle inspection work runs outside the UI thread.
- Application and native resources are disposed on exit.

### Security

- Removed unsupported safety/ban guarantees and SmartScreen-bypass guidance.
- Added DTD-prohibited XML parsing, package auditing, release checksums, CodeQL,
  and least-privilege process access.

## [1.1.0.0] - 2024-11-23

- Last original .NET Framework source release.
