# Architecture

MultiBloxy intentionally uses four focused projects rather than a large framework
or dependency-injection container.

## Projects

| Project | Target | Responsibility |
|---|---|---|
| `MultiBloxy.Core` | `net10.0` | Settings model/serialization/store, localization, guard state, recovery policy |
| `MultiBloxy` | `net10.0-windows` x64 | WinForms tray lifecycle, Windows processes, guard and handle recovery |
| `MultiBloxy.Core.Tests` | `net10.0` | Platform-neutral regression tests |
| `MultiBloxy.Windows.Tests` | `net10.0-windows` x64 | Windows layout/name/native integration tests |

## Runtime flow

1. `Program.Main` initializes WinForms and acquires a per-user, per-session
   application mutex.
2. `SettingsStore` loads `%LOCALAPPDATA%\MultiBloxy\config.xml`, or safely migrates
   a legacy config next to the executable.
3. `TrayApplicationContext` owns every UI/native resource for the application
   lifetime.
4. `RobloxGuardService` creates or disposes `ROBLOX_singletonEvent` without taking
   mutex ownership. Disposing the handle is enough; no `WaitOne/ReleaseMutex`
   cycle is needed.
5. A failed guard attempt may show one `RecoveryDialog`. The selected action is
   applied at most once, followed by at most one final enable attempt.
6. `WindowsHandleCloser` runs on a worker thread. It takes one bounded x64 system
   handle snapshot, opens each target process once with `PROCESS_DUP_HANDLE`, and
   closes only an exact final object-name match.
7. `ExitThreadCore` cancels outstanding work, disables the guard, hides/disposes
   the tray icon and menus, and frees cached GDI/native objects.

## State model

The UI has four explicit states:

- `Paused`: no Roblox guard handle is held;
- `Busy`: a user-requested operation is running;
- `Running`: the guard handle was created successfully;
- `Error`: guard creation or another critical operation failed.

`Running` describes MultiBloxy's own state. It is not proof that the current
Roblox client permits simultaneous sessions.

## Settings safety

- XML DTD processing and external resolution are disabled.
- Unknown languages and malformed booleans fall back safely.
- Only the non-destructive `Ignore` recovery action can be persisted.
- Writes use a uniquely named file in the destination directory followed by a
  same-filesystem replace/move.
- Invalid current settings are quarantined rather than repeatedly crashing startup.

## Native boundary

The Windows handle table is an unstable NT boundary. The implementation therefore:

- refuses non-Windows and non-x64 execution;
- uses `SYSTEM_HANDLE_INFORMATION_EX`-compatible pointer-sized fields;
- validates the reported entry count against the allocated buffer;
- caps the system buffer at 256 MiB and each name buffer at 1 MiB;
- checks NTSTATUS before reading data;
- validates returned Unicode pointers and lengths;
- uses exact suffix matching rather than `Contains`;
- uses `SafeProcessHandle` and a dedicated safe kernel-object handle;
- never requests `PROCESS_ALL_ACCESS`;
- returns a typed failure instead of guessing after unexpected native results.

There is still an unavoidable race between identifying and closing a handle in a
different live process. For that reason the feature is user-confirmed, isolated,
tested with a GUID object, and must fail closed whenever validation fails.
