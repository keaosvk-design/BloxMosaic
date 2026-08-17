# Test matrix

## Automated

| Area | Linux CI | Windows CI |
|---|---:|---:|
| XML parse/serialize and DTD rejection | Yes | Yes |
| Atomic save, migration, corrupt-file quarantine | Yes | Yes |
| Recovery policy and destructive-action persistence | Yes | Yes |
| English/Russian key parity and fallback | Yes | Yes |
| Solution build | No runtime validation | Yes |
| x64 native layout and exact name matching | No | Yes |
| GUID-named Event close in test process | No | Yes |
| Single-file `win-x64` publish | No | Release workflow |
| CodeQL C# security analysis | N/A | Scheduled/PR workflow |

## Manual Windows application checklist

Perform as a standard user on a supported Windows x64 installation:

1. Launch once: one tray icon appears and config/log directories are created.
2. Launch again: the second instance reports that MultiBloxy is already running.
3. Pause/resume 100 times: UI remains responsive and GDI handle count does not
   grow continuously.
4. Switch English/Russian/automatic language: every menu item updates.
5. Make config malformed: it is quarantined and the app starts with defaults.
6. Put a legacy config beside the executable: it migrates without deleting the
   legacy copy.
7. Make the settings directory read-only: the app continues and reports the save
   error in UI/logs.
8. Start/stop with no Roblox processes: no exception and an informative message.
9. Start test processes and cancel/exit during a long operation: no recursive
   dialog, repeated kill, ghost tray icon, or leaked process handle.
10. Verify `MultiBloxy.exe.sha256` against the published executable.

## Manual Roblox compatibility gate

This gate cannot be automated in GitHub Actions and must not use real credentials
in logs or CI:

1. Review current Roblox terms and anti-cheat guidance.
2. Use the official, unmodified Roblox client on a non-critical test environment.
3. Start MultiBloxy before Roblox and record whether more than one client remains
   open for at least 60 minutes.
4. Test pause/resume/reload without handle recovery.
5. Only if necessary, test the advanced recovery once and inspect logs.
6. Confirm that normal Roblox updates, login and exit still work afterwards.

If the official client intentionally blocks the mechanism or account/policy risk
cannot be accepted, the release decision is **no-go**. Do not add an injection,
driver, client patch, elevation, or evasion mechanism to force a pass.
