using System;
using System.Threading;
using System.Threading.Tasks;
using ManagedCode.Storage.AspNetExtensions.Options;
using ManagedCode.Storage.Core;
using ManagedCode.Storage.Core.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ManagedCode.Storage.AspNetExtensions.Helpers;

namespace ManagedCode.Storage.AspNetExtensions;

public static class StorageExtensions
{
    private const int MinLengthForLargeFile = 256 * 1024;

    public static async Task UploadToStorageAsync(this IStorage storage, IFormFile formFile, UploadToStorageOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        options ??= new UploadToStorageOptions();

        BlobMetadata blobMetadata = new()
        {
            Name = options.UseRandomName ? Guid.NewGuid().ToString("N").ToLowerInvariant() : formFile.FileName,
            ContentType = formFile.ContentType,
            Rewrite = options.Rewrite
        };

        if (formFile.Length > MinLengthForLargeFile)
        {
            var localFile = await formFile.ToLocalFileAsync(cancellationToken);

            await storage.UploadStreamAsync(blobMetadata, localFile.FileStream, cancellationToken);
        }
        else
        {
            using (var stream = formFile.OpenReadStream())
            {
                await storage.UploadStreamAsync(blobMetadata, stream, cancellationToken);
            }
        }
    }

    public static async Task<FileResult> DownloadAsFileResult(this IStorage storage, string blobName, CancellationToken cancellationToken = default)
    {
        var localFile = await storage.DownloadAsync(blobName, cancellationToken);

        return new FileStreamResult(localFile.FileStream, MimeHelper.GetMimeType(localFile.FileInfo.Extension));
    }
    
    public static async Task<FileResult> DownloadAsFileResult(this IStorage storage, BlobMetadata blobMetadata, CancellationToken cancellationToken = default)
    {
        var localFile = await storage.DownloadAsync(blobMetadata, cancellationToken);

        return new FileStreamResult(localFile.FileStream, MimeHelper.GetMimeType(localFile.FileInfo.Extension));
    }
}