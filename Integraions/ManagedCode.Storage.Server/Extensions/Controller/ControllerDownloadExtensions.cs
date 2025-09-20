using System.IO;
using System.Threading;
using System.Threading.Tasks;
using ManagedCode.MimeTypes;
using ManagedCode.Storage.Core;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using IResult = Microsoft.AspNetCore.Http.IResult;

namespace ManagedCode.Storage.Server.Extensions.Controller;

/// <summary>
/// Provides controller helpers for downloading content from storage.
/// </summary>
public static class ControllerDownloadExtensions
{
    /// <summary>
    /// Streams the specified blob to the caller using <see cref="IResult"/>.
    /// </summary>
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
    
    /// <summary>
    /// Downloads the specified blob as a <see cref="FileResult"/>.
    /// </summary>
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

    /// <summary>
    /// Downloads the specified blob into memory and returns a <see cref="FileContentResult"/>.
    /// </summary>
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
