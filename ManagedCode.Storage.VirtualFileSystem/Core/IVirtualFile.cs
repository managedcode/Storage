using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ManagedCode.Storage.VirtualFileSystem.Options;

namespace ManagedCode.Storage.VirtualFileSystem.Core;

/// <summary>
/// Represents a file in the virtual filesystem
/// </summary>
public interface IVirtualFile : IVfsNode
{
    /// <summary>
    /// Gets the file size in bytes
    /// </summary>
    long Size { get; }

    /// <summary>
    /// Gets the MIME content type
    /// </summary>
    string? ContentType { get; }

    /// <summary>
    /// Gets the ETag for concurrency control
    /// </summary>
    string? ETag { get; }

    /// <summary>
    /// Gets the content hash (MD5 or SHA256)
    /// </summary>
    string? ContentHash { get; }

    // Streaming Operations

    /// <summary>
    /// Opens a stream for reading the file
    /// </summary>
    /// <param name="options">Streaming options</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>A readable stream</returns>
    Task<Stream> OpenReadAsync(
        StreamOptions? options = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Opens a stream for writing to the file
    /// </summary>
    /// <param name="options">Write options including ETag check</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>A writable stream</returns>
    Task<Stream> OpenWriteAsync(
        WriteOptions? options = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Reads a specific range of bytes from the file
    /// </summary>
    /// <param name="offset">Starting offset</param>
    /// <param name="count">Number of bytes to read</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The requested bytes</returns>
    ValueTask<byte[]> ReadRangeAsync(
        long offset,
        int count,
        CancellationToken cancellationToken = default);

    // Convenience Methods

    /// <summary>
    /// Reads the entire file as bytes (use only for small files!)
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>File contents as bytes</returns>
    Task<byte[]> ReadAllBytesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Reads the file as text
    /// </summary>
    /// <param name="encoding">Text encoding (defaults to UTF-8)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>File contents as text</returns>
    Task<string> ReadAllTextAsync(
        Encoding? encoding = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Writes bytes to the file with optional ETag check
    /// </summary>
    /// <param name="bytes">Bytes to write</param>
    /// <param name="options">Write options</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task representing the async operation</returns>
    Task WriteAllBytesAsync(
        byte[] bytes,
        WriteOptions? options = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Writes text to the file with optional ETag check
    /// </summary>
    /// <param name="text">Text to write</param>
    /// <param name="encoding">Text encoding (defaults to UTF-8)</param>
    /// <param name="options">Write options</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task representing the async operation</returns>
    Task WriteAllTextAsync(
        string text,
        Encoding? encoding = null,
        WriteOptions? options = null,
        CancellationToken cancellationToken = default);

    // Metadata Operations

    /// <summary>
    /// Gets all metadata for the file (cached)
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Metadata dictionary</returns>
    ValueTask<IReadOnlyDictionary<string, string>> GetMetadataAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Sets metadata for the file with ETag check
    /// </summary>
    /// <param name="metadata">Metadata to set</param>
    /// <param name="expectedETag">Expected ETag for concurrency control</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task representing the async operation</returns>
    Task SetMetadataAsync(
        IDictionary<string, string> metadata,
        string? expectedETag = null,
        CancellationToken cancellationToken = default);

    // Large File Support

    /// <summary>
    /// Starts a multipart upload for large files
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Multipart upload handle</returns>
    Task<IMultipartUpload> StartMultipartUploadAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes this file
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if the file was deleted</returns>
    Task<bool> DeleteAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Represents a multipart upload for large files
/// </summary>
public interface IMultipartUpload : IAsyncDisposable
{
    /// <summary>
    /// Upload a part of the file
    /// </summary>
    /// <param name="partNumber">Part number (1-based)</param>
    /// <param name="data">Part data stream</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Upload part information</returns>
    Task<UploadPart> UploadPartAsync(
        int partNumber,
        Stream data,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Completes the multipart upload
    /// </summary>
    /// <param name="parts">List of uploaded parts</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task representing the async operation</returns>
    Task CompleteAsync(
        IList<UploadPart> parts,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Aborts the multipart upload
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task representing the async operation</returns>
    Task AbortAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Information about an uploaded part
/// </summary>
public class UploadPart
{
    /// <summary>
    /// Part number (1-based)
    /// </summary>
    public int PartNumber { get; set; }

    /// <summary>
    /// ETag of the uploaded part
    /// </summary>
    public string ETag { get; set; } = null!;

    /// <summary>
    /// Size of the part in bytes
    /// </summary>
    public long Size { get; set; }
}