using System;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using ManagedCode.Communication;
using ManagedCode.MimeTypes;
using ManagedCode.Storage.Core;
using ManagedCode.Storage.Core.Models;
using ManagedCode.Storage.Server.ChunkUpload;
using ManagedCode.Storage.Server.Extensions.Controller;
using ManagedCode.Storage.Server.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ManagedCode.Storage.Server.Controllers;

/// <summary>
/// Provides a reusable ASP.NET Core controller that wires storage upload, download, and chunked-transfer endpoints.
/// </summary>
public abstract class StorageControllerBase<TStorage> : ControllerBase, IStorageController where TStorage : IStorage
{
    private readonly StorageServerOptions _options;

    /// <summary>
    /// Initialises a new instance that exposes storage functionality through HTTP endpoints.
    /// </summary>
    /// <param name="storage">Storage provider used to fulfil requests.</param>
    /// <param name="chunkUploadService">Chunk upload orchestrator.</param>
    /// <param name="options">Runtime options controlling streaming behaviour.</param>
    protected StorageControllerBase(
        TStorage storage,
        ChunkUploadService chunkUploadService,
        StorageServerOptions options)
    {
        Storage = storage ?? throw new ArgumentNullException(nameof(storage));
        ChunkUploadService = chunkUploadService ?? throw new ArgumentNullException(nameof(chunkUploadService));
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    /// <summary>
    /// Gets the storage provider used by the controller.
    /// </summary>
    protected TStorage Storage { get; }

    /// <summary>
    /// Gets the chunk upload coordinator used for large uploads.
    /// </summary>
    protected ChunkUploadService ChunkUploadService { get; }

    /// <inheritdoc />
    [HttpPost("upload"), ProducesResponseType(typeof(Result<BlobMetadata>), StatusCodes.Status200OK)]
    public virtual async Task<Result<BlobMetadata>> UploadAsync([FromForm] IFormFile file, CancellationToken cancellationToken)
    {
        if (file is null)
        {
            return Result<BlobMetadata>.Fail(HttpStatusCode.BadRequest, "File payload is missing");
        }

        // Validate file size if enabled
        if (_options.EnableFileSizeValidation && _options.MaxFileSize > 0 && file.Length > _options.MaxFileSize)
        {
            return Result<BlobMetadata>.Fail(HttpStatusCode.RequestEntityTooLarge,
                $"File size {file.Length} bytes exceeds maximum allowed size of {_options.MaxFileSize} bytes");
        }

        try
        {
            return await Result.From(() => this.UploadFormFileAsync(Storage, file, cancellationToken: cancellationToken), cancellationToken);
        }
        catch (Exception ex)
        {
            return Result<BlobMetadata>.Fail(ex);
        }
    }

    /// <inheritdoc />
    [HttpPost("upload/stream"), ProducesResponseType(typeof(Result<BlobMetadata>), StatusCodes.Status200OK)]
    public virtual async Task<Result<BlobMetadata>> UploadStreamAsync(
        [FromHeader(Name = StorageServerHeaders.FileName)] string fileName,
        [FromHeader(Name = StorageServerHeaders.ContentType)] string? contentType,
        [FromHeader(Name = StorageServerHeaders.Directory)] string? directory,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            return Result<BlobMetadata>.Fail(HttpStatusCode.BadRequest, "X-File-Name header is required");
        }

        var options = new UploadOptions(fileName, directory, contentType);

        try
        {
            await using var uploadStream = Request.Body;
            var result = await Storage.UploadAsync(uploadStream, options, cancellationToken);
            return result;
        }
        catch (Exception ex)
        {
            return Result<BlobMetadata>.Fail(ex);
        }
    }

    /// <inheritdoc />
    [HttpGet("download/{*path}")]
    public virtual async Task<ActionResult> DownloadAsync([FromRoute] string path, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return Problem("File name is required", statusCode: StatusCodes.Status400BadRequest);
        }

        var result = await Storage.GetStreamAsync(path, cancellationToken);
        if (result.IsFailed)
        {
            return Problem(result.Problem?.Title ?? "File not found", statusCode: (int?)result.Problem?.StatusCode ?? StatusCodes.Status404NotFound);
        }

        return File(result.Value, MimeHelper.GetMimeType(path), path, enableRangeProcessing: _options.EnableRangeProcessing);
    }

    /// <inheritdoc />
    [HttpGet("stream/{*path}")]
    public virtual async Task<IActionResult> StreamAsync([FromRoute] string path, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return Problem("File name is required", statusCode: StatusCodes.Status400BadRequest);
        }

        var streamResult = await Storage.GetStreamAsync(path, cancellationToken);
        if (streamResult.IsFailed)
        {
            return Problem(streamResult.Problem?.Title ?? "File not found", statusCode: (int?)streamResult.Problem?.StatusCode ?? StatusCodes.Status404NotFound);
        }

        return File(streamResult.Value, MimeHelper.GetMimeType(path), fileDownloadName: null, enableRangeProcessing: _options.EnableRangeProcessing);
    }

    /// <inheritdoc />
    [HttpGet("download-bytes/{*path}")]
    public virtual async Task<ActionResult> DownloadBytesAsync([FromRoute] string path, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return Problem("File name is required", statusCode: StatusCodes.Status400BadRequest);
        }

        var streamResult = await Storage.GetStreamAsync(path, cancellationToken);
        if (streamResult.IsFailed)
        {
            return Problem(streamResult.Problem?.Title ?? "File not found", statusCode: (int?)streamResult.Problem?.StatusCode ?? StatusCodes.Status404NotFound);
        }

        return File(streamResult.Value, MimeHelper.GetMimeType(path), path, enableRangeProcessing: _options.EnableRangeProcessing);
    }

    /// <inheritdoc />
    [HttpPost("upload-chunks/upload"), ProducesResponseType(typeof(Result), StatusCodes.Status200OK)]
    public virtual async Task<Result> UploadChunkAsync([FromForm] FileUploadPayload payload, CancellationToken cancellationToken)
    {
        if (payload?.File is null)
        {
            return Result.Fail(HttpStatusCode.BadRequest, "File chunk payload is required");
        }

        if (payload.Payload is null || string.IsNullOrWhiteSpace(payload.Payload.UploadId))
        {
            return Result.Fail(HttpStatusCode.BadRequest, "UploadId is required");
        }

        // Validate chunk size if enabled
        if (_options.EnableFileSizeValidation && _options.MaxChunkSize > 0 && payload.File.Length > _options.MaxChunkSize)
        {
            return Result.Fail(HttpStatusCode.RequestEntityTooLarge,
                $"Chunk size {payload.File.Length} bytes exceeds maximum allowed chunk size of {_options.MaxChunkSize} bytes");
        }

        return await ChunkUploadService.AppendChunkAsync(payload, cancellationToken);
    }

    /// <inheritdoc />
    [HttpPost("upload-chunks/complete"), ProducesResponseType(typeof(Result<ChunkUploadCompleteResponse>), StatusCodes.Status200OK)]
    public virtual async Task<Result<ChunkUploadCompleteResponse>> CompleteChunksAsync([FromBody] ChunkUploadCompleteRequest request, CancellationToken cancellationToken)
    {
        if (request is null)
        {
            return Result<ChunkUploadCompleteResponse>.Fail(HttpStatusCode.BadRequest, "Completion request is required");
        }
        return await ChunkUploadService.CompleteAsync(request, Storage, cancellationToken);
    }

    /// <inheritdoc />
    [HttpDelete("upload-chunks/{uploadId}")]
    public virtual IActionResult AbortChunks([FromRoute] string uploadId)
    {
        if (string.IsNullOrWhiteSpace(uploadId))
        {
            return Problem("Upload id is required", statusCode: StatusCodes.Status400BadRequest);
        }

        ChunkUploadService.Abort(uploadId);
        return NoContent();
    }
}

/// <summary>
/// Provides the header constants used by the storage server endpoints.
/// </summary>
public static class StorageServerHeaders
{
    /// <summary>
    /// Header name conveying the file name supplied for stream uploads.
    /// </summary>
    public const string FileName = "X-File-Name";

    /// <summary>
    /// Header name conveying the MIME type supplied for stream uploads.
    /// </summary>
    public const string ContentType = "X-Content-Type";

    /// <summary>
    /// Header name conveying the logical directory for stream uploads.
    /// </summary>
    public const string Directory = "X-Directory";
}

/// <summary>
/// Configurable options influencing storage controller behaviour.
/// </summary>
public class StorageServerOptions
{
    /// <summary>
    /// Default threshold in bytes after which uploads are buffered to disk instead of kept in memory.
    /// </summary>
    public const int DefaultInMemoryUploadThresholdBytes = 256 * 1024;

    /// <summary>
    /// Default threshold in bytes for endpoints that return an in-memory payload (for example: <see cref="FileContentResult"/>).
    /// </summary>
    public const int DefaultInMemoryDownloadThresholdBytes = 256 * 1024;

    /// <summary>
    /// Default boundary length limit applied to multipart requests.
    /// </summary>
    public const int DefaultMultipartBoundaryLengthLimit = 70;

    /// <summary>
    /// Default maximum file size: 100 MB.
    /// </summary>
    public const long DefaultMaxFileSize = 100 * 1024 * 1024;

    /// <summary>
    /// Default maximum chunk size: 10 MB.
    /// </summary>
    public const long DefaultMaxChunkSize = 10 * 1024 * 1024;

    /// <summary>
    /// Gets or sets a value indicating whether range processing is enabled for streaming responses.
    /// </summary>
    public bool EnableRangeProcessing { get; set; } = true;

    /// <summary>
    /// Gets or sets the maximum payload size (in bytes) that will be buffered in memory before switching to a file-backed upload path.
    /// </summary>
    public int InMemoryUploadThresholdBytes { get; set; } = DefaultInMemoryUploadThresholdBytes;

    /// <summary>
    /// Gets or sets the maximum payload size (in bytes) that will be materialised in memory when an API returns the entire payload as bytes.
    /// Use streaming endpoints for larger payloads.
    /// </summary>
    public int InMemoryDownloadThresholdBytes { get; set; } = DefaultInMemoryDownloadThresholdBytes;

    /// <summary>
    /// Gets or sets the maximum allowed length for multipart boundaries when parsing raw upload streams.
    /// </summary>
    public int MultipartBoundaryLengthLimit { get; set; } = DefaultMultipartBoundaryLengthLimit;

    /// <summary>
    /// Gets or sets the maximum file size in bytes that can be uploaded. Set to 0 to disable the limit.
    /// Default is 100 MB.
    /// </summary>
    public long MaxFileSize { get; set; } = DefaultMaxFileSize;

    /// <summary>
    /// Gets or sets the maximum chunk size in bytes for chunk uploads. Set to 0 to disable the limit.
    /// Default is 10 MB.
    /// </summary>
    public long MaxChunkSize { get; set; } = DefaultMaxChunkSize;

    /// <summary>
    /// Gets or sets whether file size validation is enabled.
    /// Default is true.
    /// </summary>
    public bool EnableFileSizeValidation { get; set; } = true;
}
