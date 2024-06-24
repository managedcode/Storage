using System;
using System.Threading;
using System.Threading.Tasks;
using ManagedCode.Communication;
using ManagedCode.Storage.Core;
using ManagedCode.Storage.Core.Models;
using Microsoft.AspNetCore.Components.Forms;

namespace ManagedCode.Storage.Server;

public static class StorageBrowserFileExtensions
{
    private const int MinLengthForLargeFile = 256 * 1024;

    public static async Task<Result<BlobMetadata>> UploadToStorageAsync(this IStorage storage, IBrowserFile formFile, UploadOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        options ??= new UploadOptions(formFile.Name, mimeType: formFile.ContentType);

        if (formFile.Size > MinLengthForLargeFile)
        {
            var localFile = await formFile.ToLocalFileAsync(cancellationToken);
            return await storage.UploadAsync(localFile.FileInfo, options, cancellationToken);
        }

        await using (var stream = formFile.OpenReadStream())
        {
            return await storage.UploadAsync(stream, options, cancellationToken);
        }
    }

    public static async Task<Result<BlobMetadata>> UploadToStorageAsync(this IStorage storage, IBrowserFile formFile, Action<UploadOptions> options,
        CancellationToken cancellationToken = default)
    {
        var newOptions = new UploadOptions(formFile.Name, mimeType: formFile.ContentType);
        options.Invoke(newOptions);

        if (formFile.Size > MinLengthForLargeFile)
        {
            var localFile = await formFile.ToLocalFileAsync(cancellationToken);
            return await storage.UploadAsync(localFile.FileInfo, options, cancellationToken);
        }

        await using (var stream = formFile.OpenReadStream())
        {
            return await storage.UploadAsync(stream, options, cancellationToken);
        }
    }
}