using System;
using System.Threading;

namespace ManagedCode.Storage.Azure;

internal sealed class UploadLengthProgress : IProgress<long>
{
    private long _bytesTransferred;

    public ulong BytesTransferred => checked((ulong)Volatile.Read(ref _bytesTransferred));

    public void Report(long value)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(value);

        var current = Volatile.Read(ref _bytesTransferred);
        while (value > current)
        {
            var observed = Interlocked.CompareExchange(ref _bytesTransferred, value, current);
            if (observed == current)
                return;

            current = observed;
        }
    }
}
