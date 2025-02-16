using System.IO;
using System.Threading;
using System.Threading.Tasks;
using ManagedCode.MimeTypes;
using ManagedCode.Storage.Core;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using IResult = Microsoft.AspNetCore.Http.IResult;

namespace ManagedCode.Storage.Server.Extensions.Controller;

public static class ControllerDownloadExtensions
{
    public static async Task<IResult> DownloadAsStreamAsync(
        this ControllerBase controller,
        IStorage storage,
        string blobName,
        bool enableRangeProcessing = true,
        CancellationToken cancellationToken = default)
    {
        var result = await storage.GetStreamAsync(blobName, cancellationToken);
        if (result.IsFailed)
            throw new FileNotFoundException(blobName);
        
        return Results.Stream(result.Value, MimeHelper.GetMimeType(blobName), blobName, enableRangeProcessing: enableRangeProcessing);
    }
    
    public static async Task<FileResult> DownloadAsFileResultAsync(
        this ControllerBase controller,
        IStorage storage,
        string blobName,
        bool enableRangeProcessing = true,
        CancellationToken cancellationToken = default)
    {
        var result = await storage.GetStreamAsync(blobName, cancellationToken);
        if (result.IsFailed)
            throw new FileNotFoundException(blobName);
        
        return new FileStreamResult(result.Value, MimeHelper.GetMimeType(blobName))
        {
            FileDownloadName = blobName,
            EnableRangeProcessing = enableRangeProcessing
        };
    }

    public static async Task<FileContentResult> DownloadAsFileContentResultAsync(
        this ControllerBase controller,
        IStorage storage,
        string blobName,
        CancellationToken cancellationToken = default)
    {
        var result = await storage.DownloadAsync(blobName, cancellationToken);
        if (result.IsFailed)
            throw new FileNotFoundException(blobName);

        using var memoryStream = new MemoryStream();
        await result.Value.FileStream.CopyToAsync(memoryStream, cancellationToken);
        return new FileContentResult(memoryStream.ToArray(), MimeHelper.GetMimeType(blobName))
        {
            FileDownloadName = blobName
        };
    }
}
