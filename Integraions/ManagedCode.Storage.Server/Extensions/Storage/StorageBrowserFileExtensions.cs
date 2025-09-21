using System;
using System.Threading;
using System.Threading.Tasks;
using ManagedCode.Communication;
using ManagedCode.Storage.Core;
using ManagedCode.Storage.Core.Models;
using ManagedCode.Storage.Server.Controllers;
using ManagedCode.Storage.Server.Extensions.File;
using Microsoft.AspNetCore.Components.Forms;

namespace ManagedCode.Storage.Server.Extensions.Storage;

public static class StorageBrowserFileExtensions
{
    public static async Task<Result<BlobMetadata>> UploadToStorageAsync(this IStorage storage, IBrowserFile formFile, UploadOptions? options = null,
        CancellationToken cancellationToken = default, StorageServerOptions? serverOptions = null)
    {
        options ??= new UploadOptions(formFile.Name, mimeType: formFile.ContentType);

        var threshold = (serverOptions ?? new StorageServerOptions()).InMemoryUploadThresholdBytes;

        if (formFile.Size > threshold)
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
        CancellationToken cancellationToken = default, StorageServerOptions? serverOptions = null)
    {
        var newOptions = new UploadOptions(formFile.Name, mimeType: formFile.ContentType);
        options.Invoke(newOptions);

        var threshold = (serverOptions ?? new StorageServerOptions()).InMemoryUploadThresholdBytes;

        if (formFile.Size > threshold)
        {
            var localFile = await formFile.ToLocalFileAsync(cancellationToken);
            return await storage.UploadAsync(localFile.FileInfo, newOptions, cancellationToken);
        }

        await using (var stream = formFile.OpenReadStream())
        {
            return await storage.UploadAsync(stream, newOptions, cancellationToken);
        }
    }
}
