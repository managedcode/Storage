using System;
using System.Threading;
using System.Threading.Tasks;
using ManagedCode.Communication;
using ManagedCode.Storage.Core;
using ManagedCode.Storage.Core.Models;
using Microsoft.AspNetCore.Components.Forms;

namespace ManagedCode.Storage.AspNetExtensions;

public static class StorageBrowserFileExtensions
{
    private const int MinLengthForLargeFile = 256 * 1024;

    public static async Task<Result<BlobMetadata>> UploadToStorageAsync(this IStorage storage,
        IBrowserFile browserFile,
        UploadOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        options ??= new UploadOptions(browserFile.Name, mimeType: browserFile.ContentType);

        if (browserFile.Size > MinLengthForLargeFile)
        {
            var localFile = await browserFile.ToLocalFileAsync(cancellationToken);
            return await storage.UploadAsync(localFile.FileInfo, options, cancellationToken);
        }

        await using (var stream = browserFile.OpenReadStream())
        {
            return await storage.UploadAsync(stream, options, cancellationToken);
        }
    }

    public static async Task<Result<BlobMetadata>> UploadToStorageAsync(this IStorage storage,
        IBrowserFile browserFile,
        Action<UploadOptions> options,
        CancellationToken cancellationToken = default)
    {
        var newOptions = new UploadOptions(browserFile.Name, mimeType: browserFile.ContentType);
        options.Invoke(newOptions);

        if (browserFile.Size > MinLengthForLargeFile)
        {
            var localFile = await browserFile.ToLocalFileAsync(cancellationToken);
            return await storage.UploadAsync(localFile.FileInfo, options, cancellationToken);
        }

        await using (var stream = browserFile.OpenReadStream())
        {
            return await storage.UploadAsync(stream, options, cancellationToken);
        }
    }
}