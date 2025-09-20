using System.Net;
using System.Threading;
using System.Threading.Tasks;
using ManagedCode.Communication;
using ManagedCode.MimeTypes;
using ManagedCode.Storage.Core;
using ManagedCode.Storage.Core.Models;
using Microsoft.AspNetCore.Mvc;

namespace ManagedCode.Storage.Server.Extensions.Storage;

public static class StorageExtensions
{
    public static async Task<Result<FileResult>> DownloadAsFileResult(this IStorage storage, string blobName,
        CancellationToken cancellationToken = default)
    {
        var result = await storage.DownloadAsync(blobName, cancellationToken);

        if (result.IsFailed)
            return Result<FileResult>.Fail(result.Problem!);

        var fileStream = new FileStreamResult(result.Value!.FileStream, MimeHelper.GetMimeType(result.Value.FileInfo.Extension))
        {
            FileDownloadName = result.Value.Name
        };

        return Result<FileResult>.Succeed(fileStream);
    }

    public static async Task<Result<FileResult>> DownloadAsFileResult(this IStorage storage, BlobMetadata blobMetadata,
        CancellationToken cancellationToken = default)
    {
        var result = await storage.DownloadAsync(blobMetadata.Name, cancellationToken);

        if (result.IsFailed)
            return Result<FileResult>.Fail(result.Problem!);

        var fileStream = new FileStreamResult(result.Value!.FileStream, MimeHelper.GetMimeType(result.Value.FileInfo.Extension))
        {
            FileDownloadName = result.Value.Name
        };

        return Result<FileResult>.Succeed(fileStream);
    }
}
