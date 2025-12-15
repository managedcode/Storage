using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using ManagedCode.Communication;
using ManagedCode.Storage.Core;
using ManagedCode.Storage.Core.Models;
using ManagedCode.Storage.Server.Controllers;
using ManagedCode.Storage.Server.ChunkUpload;
using ManagedCode.Storage.Server.Extensions.File;
using ManagedCode.Storage.Server.Helpers;
using ManagedCode.Storage.Server.Models;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Net.Http.Headers;
using Microsoft.Extensions.DependencyInjection;

namespace ManagedCode.Storage.Server.Extensions.Controller;

/// <summary>
/// Provides controller helpers for uploading content into storage.
/// </summary>
public static class ControllerUploadExtensions
{
    private static StorageServerOptions ResolveServerOptions(ControllerBase controller)
    {
        var services = controller.HttpContext?.RequestServices;
        return services?.GetService<StorageServerOptions>() ?? new StorageServerOptions();
    }

    /// <summary>
    /// Uploads a form file to storage and returns blob metadata.
    /// </summary>
    public static async Task<BlobMetadata> UploadFormFileAsync(
     this ControllerBase controller,
     IStorage storage,
     IFormFile file,
     UploadOptions? uploadOptions = null,
     CancellationToken cancellationToken = default)
    {
        uploadOptions ??= new UploadOptions(file.FileName, mimeType: file.ContentType);

        var serverOptions = ResolveServerOptions(controller);
        if (file.Length > serverOptions.InMemoryUploadThresholdBytes)
        {
            await using var localFile = await file.ToLocalFileAsync(cancellationToken);
            var result = await storage.UploadAsync(localFile.FileInfo, uploadOptions, cancellationToken);
            result.ThrowIfFail();
            return result.Value!;
        }

        await using var stream = file.OpenReadStream();
        var uploadResult = await storage.UploadAsync(stream, uploadOptions, cancellationToken);
        uploadResult.ThrowIfFail();
        return uploadResult.Value!;
    }

    /// <summary>
    /// Uploads a browser file (Blazor) to storage.
    /// </summary>
    public static async Task<BlobMetadata> UploadFromBrowserFileAsync(
        this ControllerBase controller,
        IStorage storage,
        IBrowserFile file,
        UploadOptions? uploadOptions = null,
        CancellationToken cancellationToken = default)
    {
        uploadOptions ??= new UploadOptions(file.Name, mimeType: file.ContentType);

        var serverOptions = ResolveServerOptions(controller);

        if (file.Size > serverOptions.InMemoryUploadThresholdBytes)
        {
            await using var localFile = await file.ToLocalFileAsync(cancellationToken);
            var result = await storage.UploadAsync(localFile.FileInfo, uploadOptions, cancellationToken);
            result.ThrowIfFail();
            return result.Value!;
        }

        await using var stream = file.OpenReadStream();
        var uploadResult = await storage.UploadAsync(stream, uploadOptions, cancellationToken);
        uploadResult.ThrowIfFail();
        return uploadResult.Value!;
    }

    /// <summary>
    /// Appends a chunk to the current upload session.
    /// </summary>
    public static async Task<Result> UploadChunkAsync(
        this ControllerBase controller,
        ChunkUploadService chunkUploadService,
        FileUploadPayload payload,
        CancellationToken cancellationToken = default)
    {
        return await chunkUploadService.AppendChunkAsync(payload, cancellationToken);
    }

    /// <summary>
    /// Completes the chunk upload session by merging stored chunks.
    /// </summary>
    public static async Task<Result<ChunkUploadCompleteResponse>> CompleteChunkUploadAsync(
        this ControllerBase controller,
        ChunkUploadService chunkUploadService,
        IStorage storage,
        ChunkUploadCompleteRequest request,
        CancellationToken cancellationToken = default)
    {
        return await chunkUploadService.CompleteAsync(request, storage, cancellationToken);
    }

    /// <summary>
    /// Aborts an active chunk upload session.
    /// </summary>
    public static void AbortChunkUpload(
        this ControllerBase controller,
        ChunkUploadService chunkUploadService,
        string uploadId)
    {
        chunkUploadService.Abort(uploadId);
    }

    /// <summary>
    /// Uploads content from the raw request stream.
    /// </summary>
    public static async Task<BlobMetadata> UploadFromStreamAsync(
        this ControllerBase controller,
        IStorage storage,
        HttpRequest request,
        UploadOptions? uploadOptions = null,
        CancellationToken cancellationToken = default)
    {
        if (!StreamHelper.IsMultipartContentType(request.ContentType))
        {
            throw new InvalidOperationException("Not a multipart request");
        }

        var serverOptions = ResolveServerOptions(controller);

        var boundary = StreamHelper.GetBoundary(
            MediaTypeHeaderValue.Parse(request.ContentType),
            serverOptions.MultipartBoundaryLengthLimit);

        var multipartReader = new MultipartReader(boundary, request.Body);
        var section = await multipartReader.ReadNextSectionAsync(cancellationToken);

        while (section != null)
        {
            if (ContentDispositionHeaderValue.TryParse(section.ContentDisposition, out var contentDisposition)
                && StreamHelper.HasFileContentDisposition(contentDisposition))
            {
                var fileName = contentDisposition.FileName.Value;
                var contentType = section.ContentType;

                uploadOptions ??= new UploadOptions(fileName, mimeType: contentType);

                var result = await storage.UploadAsync(section.Body, uploadOptions, cancellationToken);
                result.ThrowIfFail();
                return result.Value!;
            }

            section = await multipartReader.ReadNextSectionAsync(cancellationToken);
        }

        throw new InvalidOperationException("No file found in request");
    }
}
