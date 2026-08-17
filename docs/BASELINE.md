# Modernization baseline

- Original commit: `5cbd71819467647a1356180d7da6c189ceb472f5`
- Original release tag: `v1.1.0.0`
- Source baseline date: 2024-11-23
- Modernization start: 2026-08-17
- Backup branch: `backup/before-global-modernization-2026-08-17`
- Backup tag: `backup-before-global-modernization-2026-08-17`

The original checkout passed `git fsck --full` and `git diff --check`. No build
or runtime result is recorded because the audit environment did not contain a
.NET/Mono toolchain or Windows/Roblox. The repository README and open issues also
report that Roblox may block the core behavior.

The modernization must not introduce client modification, code injection, a
kernel driver, privilege escalation, anti-cheat evasion, or silent destructive
actions. Runtime compatibility with Roblox remains an explicit manual go/no-go
gate outside automated CI.
