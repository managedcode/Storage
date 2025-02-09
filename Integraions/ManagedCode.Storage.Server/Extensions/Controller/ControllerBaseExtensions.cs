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

public static class ControllerBaseExtensions
{
    private const int DefaultMultipartBoundaryLengthLimit = 70;

    public static async Task<Result<BlobMetadata>> UploadFromFormFileAsync(
        this ControllerBase controller,
        IStorage storage,
        IFormFile file,
        UploadOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        if (options == null)
        {
            options = new UploadOptions 
            { 
                FileName = file.FileName,
                MimeType = file.ContentType
            };
        }
        
        return await storage.UploadToStorageAsync(file, options, cancellationToken);
    }

    public static async Task<Result<BlobMetadata>> UploadFromBrowserFileAsync(
        this ControllerBase controller,
        IStorage storage,
        IBrowserFile file,
        UploadOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        if (options == null)
        {
            options = new UploadOptions 
            { 
                FileName = file.Name,
                MimeType = file.ContentType
            };
        }
        
        return await storage.UploadToStorageAsync(file, options);
    }

    public static async Task<Result<BlobMetadata>> UploadFromStreamAsync(
        this ControllerBase controller,
        IStorage storage,
        HttpRequest request,
        UploadOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        if (!StreamHelper.IsMultipartContentType(request.ContentType))
        {
            return Result<BlobMetadata>.Fail(HttpStatusCode.BadRequest, "Not a multipart request");
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

                if (options == null)
                {
                    options = new UploadOptions 
                    { 
                        FileName = fileName,
                        MimeType = contentType
                    };
                }

                using var memoryStream = new MemoryStream();
                await section.Body.CopyToAsync(memoryStream, cancellationToken);
                memoryStream.Position = 0;
                
                return await storage.UploadAsync(memoryStream, options, cancellationToken);
            }

            section = await multipartReader.ReadNextSectionAsync(cancellationToken);
        }

        return Result<BlobMetadata>.Fail(HttpStatusCode.BadRequest, "No file found in request");
    }

    public static async Task<Result<FileResult>> DownloadAsFileResultAsync(
        this ControllerBase controller,
        IStorage storage,
        string blobName,
        CancellationToken cancellationToken = default)
    {
        return await storage.DownloadAsFileResult(blobName, cancellationToken);
    }
}
