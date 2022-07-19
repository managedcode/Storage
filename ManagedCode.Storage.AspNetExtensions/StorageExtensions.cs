using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using ManagedCode.MimeTypes;
using ManagedCode.Storage.AspNetExtensions.Options;
using ManagedCode.Storage.Core;
using ManagedCode.Storage.Core.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

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
            Name = options.UseRandomName ? "" : formFile.FileName,
            MimeType = formFile.ContentType,
        };

        if (formFile.Length > MinLengthForLargeFile)
        {
            var localFile = await formFile.ToLocalFileAsync(cancellationToken);
            await storage.UploadAsync(localFile.FileInfo, cancellationToken);
        }
        else
        {
            using (var stream = formFile.OpenReadStream())
            {
                await storage.UploadAsync(stream, uploadOptions =>
                {
                    uploadOptions.FileName = options.UseRandomName ? "" : formFile.FileName;
                    uploadOptions.MimeType = formFile.ContentType;

                } , cancellationToken);
            }
        }

        return blobMetadata;
    }

    public static async IAsyncEnumerable<BlobMetadata> UploadToStorageAsync(this IStorage storage, IFormFileCollection formFiles,
        UploadToStorageOptions? options = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        foreach (var formFile in formFiles)
        {
            yield return await storage.UploadToStorageAsync(formFile, options, cancellationToken);
        }
    }

    public static async Task<FileResult?> DownloadAsFileResult(this IStorage storage, string blobName, CancellationToken cancellationToken = default)
    {
        var localFile = await storage.DownloadAsync(blobName, cancellationToken);

        if (localFile is null)
        {
            return null;
        }

        return new FileStreamResult(localFile.Value.FileStream, MimeHelper.GetMimeType(localFile.Value.FileInfo.Extension))
        {
            FileDownloadName = localFile.Value.FileName
        };
    }

    public static async Task<FileResult?> DownloadAsFileResult(this IStorage storage, BlobMetadata blobMetadata,
        CancellationToken cancellationToken = default)
    {
        var localFile = await storage.DownloadAsync(blobMetadata.Name, cancellationToken);

        if (localFile is null)
        {
            return null;
        }

        return new FileStreamResult(localFile.Value.FileStream, MimeHelper.GetMimeType(localFile.Value.FileInfo.Extension))
        {
            FileDownloadName = localFile.Value.FileName
        };
    }
}