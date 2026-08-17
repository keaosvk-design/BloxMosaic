using System.Security.Cryptography;
using System.Security.Principal;
using System.Text;

namespace MultiBloxy;

internal sealed class AppSingleInstance : IDisposable
{
    private readonly Mutex? _mutex;

    private AppSingleInstance(Mutex? mutex, bool isPrimary, string? error)
    {
        _mutex = mutex;
        IsPrimary = isPrimary;
        Error = error;
    }

    public bool IsPrimary { get; }

    public string? Error { get; }

    public static AppSingleInstance TryAcquire()
    {
        try
        {
            Mutex mutex = new(
                initiallyOwned: false,
                name: BuildMutexName(),
                createdNew: out bool createdNew);

            return new AppSingleInstance(mutex, createdNew, error: null);
        }
        catch (Exception exception) when (exception is IOException
            or UnauthorizedAccessException
            or WaitHandleCannotBeOpenedException)
        {
            return new AppSingleInstance(null, isPrimary: false, exception.Message);
        }
    }

    public void Dispose() => _mutex?.Dispose();

    private static string BuildMutexName()
    {
        string identity;
        try
        {
            identity = WindowsIdentity.GetCurrent().User?.Value
                ?? Environment.UserName;
        }
        catch (SystemException)
        {
            identity = Environment.UserName;
        }

        byte[] hash = SHA256.HashData(Encoding.UTF8.GetBytes(identity));
        return $"Local\\MultiBloxy-{Convert.ToHexString(hash.AsSpan(0, 12))}";
    }
}
