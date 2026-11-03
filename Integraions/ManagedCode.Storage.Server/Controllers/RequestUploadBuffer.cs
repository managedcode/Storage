using System;
using System.IO;
using System.IO.Pipelines;
using System.Threading;
using System.Threading.Tasks;

namespace ManagedCode.Storage.Server.Controllers;

internal static class RequestUploadBuffer
{
    internal static async Task<bool> TryCopyAsync(PipeReader source, Stream destination, long maxLength, CancellationToken token)
    {
        long written = 0;
        while (true)
        {
            var read = await source.ReadAsync(token).ConfigureAwait(false);
            try
            {
                if (read.IsCanceled)
                    throw new OperationCanceledException(token);
                if (read.Buffer.Length > maxLength - written)
                    return false;

                foreach (var segment in read.Buffer)
                    await destination.WriteAsync(segment, token).ConfigureAwait(false);
                written += read.Buffer.Length;
            }
            finally
            {
                source.AdvanceTo(read.Buffer.End);
            }

            if (read.IsCompleted)
                return true;
        }
    }
}
