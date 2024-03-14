using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using ManagedCode.Communication;
using ManagedCode.Storage.Core;
using ManagedCode.Storage.Core.Models;
using Microsoft.AspNetCore.Http;

namespace ManagedCode.Storage.Server.Extensions;

public static class StorageFromFileExtensions
{
    private const int MinLengthForLargeFile = 256 * 1024;

    public static async Task<Result<BlobMetadata>> UploadToStorageAsync(this IStorage storage,
        IFormFile formFile,
        UploadOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        options ??= new UploadOptions(formFile.FileName, mimeType: formFile.ContentType);

        if (formFile.Length > MinLengthForLargeFile)
        {
            var localFile = await formFile.ToLocalFileAsync(cancellationToken);
            return await storage.UploadAsync(localFile.FileInfo, options, cancellationToken);
        }

        await using (var stream = formFile.OpenReadStream())
        {
            return await storage.UploadAsync(stream, options, cancellationToken);
        }
    }

    public static async Task<Result<BlobMetadata>> UploadToStorageAsync(this IStorage storage,
        IFormFile formFile,
        Action<UploadOptions> options,
        CancellationToken cancellationToken = default)
    {
        var newOptions = new UploadOptions(formFile.FileName, mimeType: formFile.ContentType);
        options.Invoke(newOptions);

        if (formFile.Length > MinLengthForLargeFile)
        {
            var localFile = await formFile.ToLocalFileAsync(cancellationToken);
            return await storage.UploadAsync(localFile.FileInfo, options, cancellationToken);
        }

        await using (var stream = formFile.OpenReadStream())
        {
            return await storage.UploadAsync(stream, options, cancellationToken);
        }
    }

    public static async IAsyncEnumerable<Result<BlobMetadata>> UploadToStorageAsync(this IStorage storage,
        IFormFileCollection formFiles,
        UploadOptions? options = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        foreach (var formFile in formFiles)
        {
            yield return await storage.UploadToStorageAsync(formFile, options, cancellationToken);
        }
    }

    public static async IAsyncEnumerable<Result<BlobMetadata>> UploadToStorageAsync(this IStorage storage,
        IFormFileCollection formFiles,
        Action<UploadOptions> options,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        foreach (var formFile in formFiles)
        {
            yield return await storage.UploadToStorageAsync(formFile, options, cancellationToken);
        }
    }
}