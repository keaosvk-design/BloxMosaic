using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace MultiBloxy;

internal sealed record HandleCloseResult(
    bool Supported,
    int ProcessCount,
    int ScannedHandles,
    int ClosedHandles,
    int FailedInspections,
    bool Cancelled,
    string? Error = null);

internal sealed class WindowsHandleCloser
{
    private const int SystemExtendedHandleInformation = 64;
    private const int ObjectNameInformation = 1;
    private const int StatusInfoLengthMismatch = unchecked((int)0xC0000004);
    private const int StatusBufferTooSmall = unchecked((int)0xC0000023);
    private const uint ProcessDuplicateHandle = 0x0040;
    private const uint DuplicateCloseSource = 0x0001;
    private const uint DuplicateSameAccess = 0x0002;
    private const int InitialSystemBufferBytes = 1 * 1024 * 1024;
    private const int MaximumSystemBufferBytes = 256 * 1024 * 1024;
    private const int MaximumNameBufferBytes = 1 * 1024 * 1024;

    private readonly string _processName;
    private readonly string _targetObjectName;

    public WindowsHandleCloser(string processName, string targetObjectName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(processName);
        ArgumentException.ThrowIfNullOrWhiteSpace(targetObjectName);
        _processName = processName;
        _targetObjectName = targetObjectName;
    }

    public Task<HandleCloseResult> CloseMatchingHandlesAsync(CancellationToken cancellationToken) =>
        Task.Run(() => CloseMatchingHandles(cancellationToken), cancellationToken);

    internal static int ExtendedHandleEntrySize =>
        Marshal.SizeOf<SystemHandleTableEntryInfoEx>();

    internal static int ExtendedHandleHeaderSize => IntPtr.Size * 2;

    internal static bool IsTargetObjectName(string? objectName, string targetObjectName)
    {
        if (string.IsNullOrEmpty(objectName))
        {
            return false;
        }

        return string.Equals(objectName, targetObjectName, StringComparison.Ordinal)
            || objectName.EndsWith(
                $"\\{targetObjectName}",
                StringComparison.Ordinal);
    }

    private HandleCloseResult CloseMatchingHandles(CancellationToken cancellationToken)
    {
        if (!OperatingSystem.IsWindows() || !Environment.Is64BitProcess)
        {
            return new HandleCloseResult(
                Supported: false,
                ProcessCount: 0,
                ScannedHandles: 0,
                ClosedHandles: 0,
                FailedInspections: 0,
                Cancelled: false,
                Error: "Handle recovery is supported only by the 64-bit Windows build.");
        }

        Process[] processes;
        try
        {
            processes = Process.GetProcessesByName(_processName);
        }
        catch (Exception exception) when (exception is InvalidOperationException
            or System.ComponentModel.Win32Exception)
        {
            return new HandleCloseResult(
                Supported: false,
                ProcessCount: 0,
                ScannedHandles: 0,
                ClosedHandles: 0,
                FailedInspections: 0,
                Cancelled: false,
                Error: exception.Message);
        }

        HashSet<int> processIds = new();
        try
        {
            foreach (Process process in processes)
            {
                try
                {
                    processIds.Add(process.Id);
                }
                catch (InvalidOperationException)
                {
                    // The process exited between discovery and reading its ID.
                }
            }
        }
        finally
        {
            foreach (Process process in processes)
            {
                process.Dispose();
            }
        }

        if (processIds.Count == 0)
        {
            return new HandleCloseResult(
                Supported: true,
                ProcessCount: 0,
                ScannedHandles: 0,
                ClosedHandles: 0,
                FailedInspections: 0,
                Cancelled: false);
        }

        using EventWaitHandle eventTypeProbe = new(
            initialState: false,
            EventResetMode.ManualReset);
        ulong eventTypeProbeHandle = unchecked(
            (ulong)eventTypeProbe.SafeWaitHandle.DangerousGetHandle().ToInt64());

        using UnmanagedBuffer systemBuffer = new(InitialSystemBufferBytes);
        int queryStatus;
        uint requiredBytes;

        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();
            queryStatus = NtQuerySystemInformation(
                SystemExtendedHandleInformation,
                systemBuffer.Pointer,
                checked((uint)systemBuffer.Length),
                out requiredBytes);

            if (queryStatus != StatusInfoLengthMismatch)
            {
                break;
            }

            long requested = Math.Max(
                requiredBytes,
                checked((long)systemBuffer.Length * 2));
            if (requested > MaximumSystemBufferBytes)
            {
                return Failure("The Windows handle table exceeded the safety limit.");
            }

            systemBuffer.Resize(checked((int)requested));
        }

        if (!NtSuccess(queryStatus))
        {
            return Failure($"NtQuerySystemInformation failed with NTSTATUS 0x{queryStatus:X8}.");
        }

        ulong reportedCount = unchecked((ulong)Marshal.ReadInt64(systemBuffer.Pointer));
        int entrySize = ExtendedHandleEntrySize;
        int headerSize = ExtendedHandleHeaderSize;
        ulong maximumEntries = checked((ulong)(systemBuffer.Length - headerSize) / (ulong)entrySize);
        if (reportedCount > maximumEntries || reportedCount > int.MaxValue)
        {
            return Failure("Windows returned an invalid handle-table length.");
        }

        int handleCount = checked((int)reportedCount);
        ushort? eventObjectTypeIndex = null;
        for (int index = 0; index < handleCount; index++)
        {
            IntPtr entryPointer = IntPtr.Add(
                systemBuffer.Pointer,
                checked(headerSize + (index * entrySize)));
            SystemHandleTableEntryInfoEx entry =
                Marshal.PtrToStructure<SystemHandleTableEntryInfoEx>(entryPointer);
            if (entry.UniqueProcessId.ToUInt64() == (ulong)Environment.ProcessId
                && entry.HandleValue.ToUInt64() == eventTypeProbeHandle)
            {
                eventObjectTypeIndex = entry.ObjectTypeIndex;
                break;
            }
        }

        if (eventObjectTypeIndex is null)
        {
            return Failure("The Windows Event object type could not be identified safely.");
        }

        int scanned = 0;
        int closed = 0;
        int failed = 0;
        Dictionary<int, SafeProcessHandle> processHandles = new();
        HashSet<int> inaccessibleProcesses = new();

        try
        {
            for (int index = 0; index < handleCount; index++)
            {
                if ((index & 0xFF) == 0)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                }

                IntPtr entryPointer = IntPtr.Add(
                    systemBuffer.Pointer,
                    checked(headerSize + (index * entrySize)));
                SystemHandleTableEntryInfoEx entry =
                    Marshal.PtrToStructure<SystemHandleTableEntryInfoEx>(entryPointer);

                // Querying names for every kernel-object type is both expensive and
                // unsafe because some NtQueryObject calls may block. Identify the
                // Event type dynamically with a local probe and inspect only Events.
                if (entry.ObjectTypeIndex != eventObjectTypeIndex.Value)
                {
                    continue;
                }

                ulong processIdValue = entry.UniqueProcessId.ToUInt64();
                if (processIdValue > int.MaxValue)
                {
                    continue;
                }

                int processId = (int)processIdValue;
                if (!processIds.Contains(processId))
                {
                    continue;
                }

                scanned++;
                if (inaccessibleProcesses.Contains(processId))
                {
                    failed++;
                    continue;
                }

                if (!processHandles.TryGetValue(processId, out SafeProcessHandle? processHandle))
                {
                    processHandle = OpenProcess(ProcessDuplicateHandle, inheritHandle: false, processId);
                    if (processHandle.IsInvalid)
                    {
                        processHandle.Dispose();
                        inaccessibleProcesses.Add(processId);
                        failed++;
                        continue;
                    }

                    processHandles.Add(processId, processHandle);
                }

                IntPtr sourceHandle = new(unchecked((long)entry.HandleValue.ToUInt64()));
                if (!DuplicateHandle(
                    processHandle,
                    sourceHandle,
                    GetCurrentProcess(),
                    out SafeKernelObjectHandle? duplicate,
                    desiredAccess: 0,
                    inheritHandle: false,
                    options: DuplicateSameAccess)
                    || duplicate is null
                    || duplicate.IsInvalid)
                {
                    duplicate?.Dispose();
                    failed++;
                    continue;
                }

                using (duplicate)
                {
                    string? objectName = TryGetObjectName(duplicate);
                    if (!IsTargetObjectName(objectName, _targetObjectName))
                    {
                        continue;
                    }

                    if (DuplicateHandle(
                        processHandle,
                        sourceHandle,
                        GetCurrentProcess(),
                        out SafeKernelObjectHandle? closedSourceDuplicate,
                        desiredAccess: 0,
                        inheritHandle: false,
                        options: DuplicateCloseSource | DuplicateSameAccess)
                        && closedSourceDuplicate is not null
                        && !closedSourceDuplicate.IsInvalid)
                    {
                        closedSourceDuplicate.Dispose();
                        closed++;
                    }
                    else
                    {
                        closedSourceDuplicate?.Dispose();
                        failed++;
                    }
                }
            }
        }
        catch (OperationCanceledException)
        {
            return new HandleCloseResult(
                Supported: true,
                ProcessCount: processIds.Count,
                ScannedHandles: scanned,
                ClosedHandles: closed,
                FailedInspections: failed,
                Cancelled: true);
        }
        finally
        {
            foreach (SafeProcessHandle processHandle in processHandles.Values)
            {
                processHandle.Dispose();
            }
        }

        return new HandleCloseResult(
            Supported: true,
            ProcessCount: processIds.Count,
            ScannedHandles: scanned,
            ClosedHandles: closed,
            FailedInspections: failed,
            Cancelled: false);

        HandleCloseResult Failure(string error) => new(
            Supported: false,
            ProcessCount: processIds.Count,
            ScannedHandles: 0,
            ClosedHandles: 0,
            FailedInspections: 0,
            Cancelled: false,
            Error: error);
    }

    private static string? TryGetObjectName(SafeKernelObjectHandle handle)
    {
        int bufferSize = 1024;
        using UnmanagedBuffer buffer = new(bufferSize);

        for (int attempt = 0; attempt < 3; attempt++)
        {
            int status = NtQueryObject(
                handle.DangerousGetHandle(),
                ObjectNameInformation,
                buffer.Pointer,
                checked((uint)buffer.Length),
                out uint requiredBytes);

            if (status is StatusInfoLengthMismatch or StatusBufferTooSmall)
            {
                long requested = Math.Max(requiredBytes, checked((long)buffer.Length * 2));
                if (requested > MaximumNameBufferBytes)
                {
                    return null;
                }

                buffer.Resize(checked((int)requested));
                continue;
            }

            if (!NtSuccess(status))
            {
                return null;
            }

            UnicodeString name = Marshal.PtrToStructure<UnicodeString>(buffer.Pointer);
            if (name.Length == 0 || name.Buffer == IntPtr.Zero || (name.Length & 1) != 0)
            {
                return null;
            }

            long bufferStart = buffer.Pointer.ToInt64();
            long bufferEnd = checked(bufferStart + buffer.Length);
            long nameStart = name.Buffer.ToInt64();
            long nameEnd = checked(nameStart + name.Length);
            if (nameStart < bufferStart || nameEnd > bufferEnd)
            {
                return null;
            }

            return Marshal.PtrToStringUni(name.Buffer, name.Length / sizeof(char));
        }

        return null;
    }

    private static bool NtSuccess(int status) => status >= 0;

    [StructLayout(LayoutKind.Sequential)]
    internal struct SystemHandleTableEntryInfoEx
    {
        public IntPtr Object;
        public UIntPtr UniqueProcessId;
        public UIntPtr HandleValue;
        public uint GrantedAccess;
        public ushort CreatorBackTraceIndex;
        public ushort ObjectTypeIndex;
        public uint HandleAttributes;
        public uint Reserved;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct UnicodeString
    {
        public ushort Length;
        public ushort MaximumLength;
        public IntPtr Buffer;
    }

    private sealed class UnmanagedBuffer : IDisposable
    {
        public UnmanagedBuffer(int length)
        {
            Resize(length);
        }

        public IntPtr Pointer { get; private set; }

        public int Length { get; private set; }

        public void Resize(int length)
        {
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(length);
            IntPtr replacement = Marshal.AllocHGlobal(length);
            if (Pointer != IntPtr.Zero)
            {
                Marshal.FreeHGlobal(Pointer);
            }

            Pointer = replacement;
            Length = length;
        }

        public void Dispose()
        {
            if (Pointer == IntPtr.Zero)
            {
                return;
            }

            Marshal.FreeHGlobal(Pointer);
            Pointer = IntPtr.Zero;
            Length = 0;
        }
    }

    private sealed class SafeKernelObjectHandle : SafeHandleZeroOrMinusOneIsInvalid
    {
        public SafeKernelObjectHandle()
            : base(ownsHandle: true)
        {
        }

        protected override bool ReleaseHandle() => CloseHandle(handle);
    }

    [DllImport("ntdll.dll")]
    private static extern int NtQuerySystemInformation(
        int systemInformationClass,
        IntPtr systemInformation,
        uint systemInformationLength,
        out uint returnLength);

    [DllImport("ntdll.dll")]
    private static extern int NtQueryObject(
        IntPtr handle,
        int objectInformationClass,
        IntPtr objectInformation,
        uint objectInformationLength,
        out uint returnLength);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern SafeProcessHandle OpenProcess(
        uint desiredAccess,
        [MarshalAs(UnmanagedType.Bool)] bool inheritHandle,
        int processId);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool DuplicateHandle(
        SafeProcessHandle sourceProcessHandle,
        IntPtr sourceHandle,
        IntPtr targetProcessHandle,
        out SafeKernelObjectHandle? targetHandle,
        uint desiredAccess,
        [MarshalAs(UnmanagedType.Bool)] bool inheritHandle,
        uint options);

    [DllImport("kernel32.dll")]
    private static extern IntPtr GetCurrentProcess();

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool CloseHandle(IntPtr handle);
}
