using System.Diagnostics;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace MultiBloxy.Windows.Tests;

[TestClass]
public sealed class WindowsHandleCloserTests
{
    [TestMethod]
    public void ExtendedHandleLayout_IsCorrectForX64()
    {
        Assert.AreEqual(8, IntPtr.Size);
        Assert.AreEqual(16, WindowsHandleCloser.ExtendedHandleHeaderSize);
        Assert.AreEqual(40, WindowsHandleCloser.ExtendedHandleEntrySize);
    }

    [TestMethod]
    [DataRow("ROBLOX_singletonEvent", true)]
    [DataRow("\\Sessions\\1\\BaseNamedObjects\\ROBLOX_singletonEvent", true)]
    [DataRow("prefixROBLOX_singletonEvent", false)]
    [DataRow("ROBLOX_singletonEvent-suffix", false)]
    [DataRow(null, false)]
    public void ObjectNameMatching_RequiresAnExactFinalComponent(
        string? objectName,
        bool expected)
    {
        Assert.AreEqual(
            expected,
            WindowsHandleCloser.IsTargetObjectName(
                objectName,
                "ROBLOX_singletonEvent"));
    }

    [TestMethod]
    [TestCategory("WindowsIntegration")]
    public async Task CloseMatchingHandles_ClosesOnlyTheUniqueTestEvent()
    {
        if (!OperatingSystem.IsWindows() || !Environment.Is64BitProcess)
        {
            Assert.Inconclusive("This integration test requires 64-bit Windows.");
        }

        string objectName = $"MultiBloxy-Test-{Guid.NewGuid():N}";
        using EventWaitHandle waitHandle = new(
            initialState: false,
            EventResetMode.ManualReset,
            $"Local\\{objectName}");
        using Process currentProcess = Process.GetCurrentProcess();
        WindowsHandleCloser closer = new(currentProcess.ProcessName, objectName);

        HandleCloseResult result = await closer.CloseMatchingHandlesAsync(
            CancellationToken.None);

        if (result.ClosedHandles > 0)
        {
            // The native operation closed the OS handle. Prevent SafeHandle from
            // closing a numeric handle that Windows may already have reused.
            waitHandle.SafeWaitHandle.SetHandleAsInvalid();
        }

        Assert.IsTrue(result.Supported, result.Error);
        Assert.IsFalse(result.Cancelled);
        Assert.IsTrue(result.ClosedHandles >= 1, result.Error);
    }
}
