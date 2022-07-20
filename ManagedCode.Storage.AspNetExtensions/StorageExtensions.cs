using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using ManagedCode.Communication;
using ManagedCode.MimeTypes;
using ManagedCode.Storage.Core;
using ManagedCode.Storage.Core.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ManagedCode.Storage.AspNetExtensions;

public static class StorageExtensions
{
    private const int MinLengthForLargeFile = 256 * 1024;

    public static async Task<Result<string>> UploadToStorageAsync(this IStorage storage, IFormFile formFile, UploadOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        options ??= new UploadOptions(mimeType: formFile.ContentType);

        if (formFile.Length > MinLengthForLargeFile)
        {
            var localFile = await formFile.ToLocalFileAsync(cancellationToken);
            return await storage.UploadAsync(localFile.FileInfo, cancellationToken);
        }

        using (var stream = formFile.OpenReadStream())
        {
            return await storage.UploadAsync(stream, options, cancellationToken);
        }
    }

    public static async IAsyncEnumerable<Result<string>> UploadToStorageAsync(this IStorage storage, IFormFileCollection formFiles,
        UploadOptions? options = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        foreach (var formFile in formFiles)
        {
            yield return await storage.UploadToStorageAsync(formFile, options, cancellationToken);
        }
    }

    public static async Task<Result<FileResult>> DownloadAsFileResult(this IStorage storage, string blobName,
        CancellationToken cancellationToken = default)
    {
        var result = await storage.DownloadAsync(blobName, cancellationToken);

        if (result.IsError)
        {
            return Result<FileResult>.Fail(result.Error);
        }

        var fileStream = new FileStreamResult(result.Value!.FileStream, MimeHelper.GetMimeType(result.Value.FileInfo.Extension))
        {
            FileDownloadName = result.Value.FileName
        };

        return Result<FileResult>.Succeed(fileStream);
    }

    public static async Task<Result<FileResult>> DownloadAsFileResult(this IStorage storage, BlobMetadata blobMetadata,
        CancellationToken cancellationToken = default)
    {
        var result = await storage.DownloadAsync(blobMetadata.Name, cancellationToken);

        if (result.IsError)
        {
            return Result.Fail<FileResult>(result.Error);
        }

        var fileStream = new FileStreamResult(result.Value!.FileStream, MimeHelper.GetMimeType(result.Value.FileInfo.Extension))
        {
            FileDownloadName = result.Value.FileName
        };

        return Result<FileResult>.Succeed(fileStream);
    }
}