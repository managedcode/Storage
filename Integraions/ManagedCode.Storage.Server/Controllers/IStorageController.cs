using System.Threading;
using System.Threading.Tasks;
using ManagedCode.Communication;
using ManagedCode.Storage.Core.Models;
using ManagedCode.Storage.Server.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ManagedCode.Storage.Server.Controllers;

/// <summary>
/// Describes the recommended set of endpoints for storage-backed controllers.
/// Implementations can inherit <see cref="StorageControllerBase{TStorage}"/> or compose their own controllers using the extension methods.
/// </summary>
public interface IStorageController
{
    /// <summary>
    /// Uploads a single file using a multipart/form-data request.
    /// </summary>
    Task<Result<BlobMetadata>> UploadAsync(IFormFile file, CancellationToken cancellationToken);

    /// <summary>
    /// Uploads a file using the raw request body stream and metadata headers.
    /// </summary>
    Task<Result<BlobMetadata>> UploadStreamAsync(string fileName, string? contentType, string? directory, CancellationToken cancellationToken);

    /// <summary>
    /// Returns a file download result for the specified path.
    /// </summary>
    Task<ActionResult> DownloadAsync(string path, CancellationToken cancellationToken);

    /// <summary>
    /// Streams file content to the caller, enabling range processing when supported.
    /// </summary>
    Task<IActionResult> StreamAsync(string path, CancellationToken cancellationToken);

    /// <summary>
    /// Materialises a file into memory and returns it as a <see cref="FileContentResult"/>.
    /// </summary>
    Task<ActionResult> DownloadBytesAsync(string path, CancellationToken cancellationToken);

    /// <summary>
    /// Persists a chunk within an active chunked-upload session.
    /// </summary>
    Task<Result> UploadChunkAsync(FileUploadPayload payload, CancellationToken cancellationToken);

    /// <summary>
    /// Completes an upload session by merging chunks and optionally committing to backing storage.
    /// </summary>
    Task<Result<ChunkUploadCompleteResponse>> CompleteChunksAsync(ChunkUploadCompleteRequest request, CancellationToken cancellationToken);

    /// <summary>
    /// Aborts an active chunked upload and removes temporary state.
    /// </summary>
    IActionResult AbortChunks(string uploadId);
}
