using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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

    public static async Task<BlobMetadata> UploadToStorageAsync(this IStorage storage, IFormFile formFile, UploadToStorageOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        options ??= new UploadToStorageOptions();

        var extension = Path.GetExtension(formFile.FileName);

        BlobMetadata blobMetadata = new()
        {
            Name = options.UseRandomName ? $"{Guid.NewGuid().ToString("N").ToLowerInvariant()}{extension}" : formFile.FileName,
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

        return blobMetadata;
    }

    public static async Task<IEnumerable<BlobMetadata>> UploadToStorageAsync(this IStorage storage, IFormFileCollection formFiles,
        UploadToStorageOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var tasks = formFiles
            .Select(formFile => storage.UploadToStorageAsync(formFile, options, cancellationToken))
            .ToList();

        var blobs = await Task.WhenAll(tasks);

        return blobs.ToList();
    }

    public static async Task<FileResult> DownloadAsFileResult(this IStorage storage, string blobName, CancellationToken cancellationToken = default)
    {
        var localFile = await storage.DownloadAsync(blobName, cancellationToken);

        return new FileStreamResult(localFile.FileStream, MimeHelper.GetMimeType(localFile.FileInfo.Extension))
        {
            FileDownloadName = localFile.FileName
        };
    }

    public static async Task<FileResult> DownloadAsFileResult(this IStorage storage, BlobMetadata blobMetadata,
        CancellationToken cancellationToken = default)
    {
        var localFile = await storage.DownloadAsync(blobMetadata, cancellationToken);

        return new FileStreamResult(localFile.FileStream, MimeHelper.GetMimeType(localFile.FileInfo.Extension))
        {
            FileDownloadName = localFile.FileName
        };
    }
}