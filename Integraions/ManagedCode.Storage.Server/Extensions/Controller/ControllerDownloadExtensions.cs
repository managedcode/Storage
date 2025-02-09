using System.IO;
using System.Threading;
using System.Threading.Tasks;
using ManagedCode.Communication;
using ManagedCode.MimeTypes;
using ManagedCode.Storage.Core;
using ManagedCode.Storage.Core.Models;
using Microsoft.AspNetCore.Mvc;

namespace ManagedCode.Storage.Server.Extensions;

public static class ControllerDownloadExtensions
{
    public static async Task<Result<FileResult>> DownloadFileAsync(
        this ControllerBase controller,
        IStorage storage,
        string blobName,
        CancellationToken cancellationToken = default)
    {
        var result = await storage.DownloadAsync(blobName, cancellationToken);
        if (result.IsFailed)
            return Result<FileResult>.Fail(result.Errors);

        var fileStream = new FileStreamResult(result.Value!.FileStream, 
            MimeHelper.GetMimeType(result.Value.FileInfo.Extension))
        {
            FileDownloadName = result.Value.Name
        };

        return Result<FileResult>.Succeed(fileStream);
    }

    public static async Task<Result<FileResult>> StreamVideoAsync(
        this ControllerBase controller,
        IStorage storage,
        string blobName,
        CancellationToken cancellationToken = default)
    {
        var result = await storage.DownloadAsync(blobName, cancellationToken);
        if (result.IsFailed)
            return Result<FileResult>.Fail(result.Errors);

        var fileStream = new FileStreamResult(result.Value!.FileStream, 
            MimeHelper.GetMimeType(result.Value.FileInfo.Extension))
        {
            EnableRangeProcessing = true,
            FileDownloadName = result.Value.Name
        };

        return Result<FileResult>.Succeed(fileStream);
    }

    public static async Task<Result<FileContentResult>> DownloadAsByteArrayAsync(
        this ControllerBase controller,
        IStorage storage,
        string blobName,
        CancellationToken cancellationToken = default)
    {
        var result = await storage.DownloadAsync(blobName, cancellationToken);
        if (result.IsFailed)
            return Result<FileContentResult>.Fail(result.Errors);

        using var memoryStream = new MemoryStream();
        await result.Value!.FileStream.CopyToAsync(memoryStream, cancellationToken);
        
        var fileContent = new FileContentResult(memoryStream.ToArray(), 
            MimeHelper.GetMimeType(result.Value.FileInfo.Extension))
        {
            FileDownloadName = result.Value.Name
        };

        return Result<FileContentResult>.Succeed(fileContent);
    }
}
