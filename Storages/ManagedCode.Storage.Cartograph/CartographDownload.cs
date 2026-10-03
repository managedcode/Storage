using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace ManagedCode.Storage.Cartograph;

internal static class CartographDownload
{
    private const int BufferSize = 80 * 1024;

    public static async Task CopyToAsync(Stream source, string destinationPath, CancellationToken cancellationToken)
    {
        var stagingPath = destinationPath + '.' + Guid.NewGuid().ToString("N") + ".download";
        try
        {
            await using (var destination = new FileStream(stagingPath, FileMode.CreateNew, FileAccess.Write,
                             FileShare.None, BufferSize, FileOptions.Asynchronous))
                await source.CopyToAsync(destination, cancellationToken).ConfigureAwait(false);

            cancellationToken.ThrowIfCancellationRequested();
            File.Move(stagingPath, destinationPath, overwrite: true);
        }
        finally
        {
            File.Delete(stagingPath);
        }
    }
}
