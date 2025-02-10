using System;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using ManagedCode.Communication;
using ManagedCode.Storage.Core;
using ManagedCode.Storage.Core.Models;
using ManagedCode.Storage.Server.Helpers;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Net.Http.Headers;

namespace ManagedCode.Storage.Server.Extensions;

public static class ControllerUploadExtensions
{
    private const int DefaultMultipartBoundaryLengthLimit = 70;
    private const int MinLengthForLargeFile = 256 * 1024;

   public static async Task<BlobMetadata> UploadFormFileAsync(
    this ControllerBase controller,
    IStorage storage,
    IFormFile file,
    UploadOptions? options = null,
    CancellationToken cancellationToken = default)
{
    options ??= new UploadOptions(file.FileName, mimeType: file.ContentType);

    if (file.Length > MinLengthForLargeFile)
    {
        var localFile = await file.ToLocalFileAsync(cancellationToken);
        var result = await storage.UploadAsync(localFile.FileInfo, options, cancellationToken);
        result.ThrowIfFail();
        return result.Value;
    }
    else
    {
        await using var stream = file.OpenReadStream();
        var result = await storage.UploadAsync(stream, options, cancellationToken);
        result.ThrowIfFail();
        return result.Value;
    }
}

public static async Task<BlobMetadata> UploadFromBrowserFileAsync(
    this ControllerBase controller,
    IStorage storage,
    IBrowserFile file,
    UploadOptions? options = null,
    CancellationToken cancellationToken = default)
{
    options ??= new UploadOptions(file.Name, mimeType: file.ContentType);

    if (file.Size > MinLengthForLargeFile)
    {
        var localFile = await file.ToLocalFileAsync(cancellationToken);
        var result = await storage.UploadAsync(localFile.FileInfo, options, cancellationToken);
        result.ThrowIfFail();
        return result.Value;
    }
    else
    {
        await using var stream = file.OpenReadStream();
        var result =  await storage.UploadAsync(stream, options, cancellationToken);
        result.ThrowIfFail();
        return result.Value;
    }
}

public static async Task<BlobMetadata> UploadFromStreamAsync(
    this ControllerBase controller,
    IStorage storage,
    HttpRequest request,
    UploadOptions? options = null,
    CancellationToken cancellationToken = default)
{
    if (!StreamHelper.IsMultipartContentType(request.ContentType))
    {
        throw new InvalidOperationException("Not a multipart request");
    }

    var boundary = StreamHelper.GetBoundary(
        MediaTypeHeaderValue.Parse(request.ContentType),
        DefaultMultipartBoundaryLengthLimit);

    var multipartReader = new MultipartReader(boundary, request.Body);
    var section = await multipartReader.ReadNextSectionAsync(cancellationToken);

    while (section != null)
    {
        if (ContentDispositionHeaderValue.TryParse(section.ContentDisposition, out var contentDisposition)
            && StreamHelper.HasFileContentDisposition(contentDisposition))
        {
            var fileName = contentDisposition.FileName.Value;
            var contentType = section.ContentType;

            options ??= new UploadOptions(fileName, mimeType: contentType);

            using var memoryStream = new MemoryStream();
            await section.Body.CopyToAsync(memoryStream, cancellationToken);
            memoryStream.Position = 0;

            var result = await storage.UploadAsync(memoryStream, options, cancellationToken);
            result.ThrowIfFail();
            return result.Value;
        }

        section = await multipartReader.ReadNextSectionAsync(cancellationToken);
    }

    throw new InvalidOperationException("No file found in request");
}
}
