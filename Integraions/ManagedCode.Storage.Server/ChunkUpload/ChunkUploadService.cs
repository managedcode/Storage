using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ManagedCode.Communication;
using ManagedCode.Storage.Core;
using ManagedCode.Storage.Core.Helpers;
using ManagedCode.Storage.Core.Models;
using ManagedCode.Storage.Server.Models;

namespace ManagedCode.Storage.Server.ChunkUpload;

/// <summary>
/// Coordinates multi-part uploads by persisting temporary chunks and merging them on completion.
/// </summary>
public sealed class ChunkUploadService
{
    private const int MergeBufferSize = 81920;
    private readonly ChunkUploadOptions _options;
    private readonly ConcurrentDictionary<string, ChunkUploadSession> _sessions = new();

    /// <summary>
    /// Initialises the service with the specified options.
    /// </summary>
    /// <param name="options">Chunk upload options.</param>
    public ChunkUploadService(ChunkUploadOptions options)
    {
        _options = options;
        Directory.CreateDirectory(_options.TempPath);
    }

    /// <summary>
    /// Appends a chunk to an upload session, creating the session if necessary.
    /// </summary>
    /// <param name="payload">Incoming file payload.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A result that indicates whether the chunk was accepted.</returns>
    public async Task<Result> AppendChunkAsync(FileUploadPayload payload, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(payload);
        ArgumentNullException.ThrowIfNull(payload.File);
        ArgumentNullException.ThrowIfNull(payload.Payload);

        var descriptor = payload.Payload;
        var uploadId = ChunkUploadDescriptor.ResolveUploadId(descriptor);

        // Sanitize upload ID to prevent path traversal
        var sanitizedUploadId = SanitizeUploadId(uploadId);

        var session = _sessions.GetOrAdd(sanitizedUploadId, static (key, state) =>
        {
            var descriptor = state.Payload;
            var workingDirectory = Path.Combine(state.Options.TempPath, key);
            Directory.CreateDirectory(workingDirectory);
            return new ChunkUploadSession(
                key,
                descriptor.FileName ?? descriptor.UploadId,
                descriptor.ContentType,
                descriptor.TotalChunks,
                descriptor.ChunkSize,
                descriptor.FileSize,
                workingDirectory);
        }, (Payload: descriptor, Options: _options));

        if (_options.MaxActiveSessions > 0 && _sessions.Count > _options.MaxActiveSessions)
        {
            return Result.Fail("Maximum number of parallel chunk uploads exceeded");
        }

        var chunkFilePath = Path.Combine(session.WorkingDirectory, $"{descriptor.ChunkIndex:D6}.part");

        await using (var targetStream = new FileStream(chunkFilePath, FileMode.Create, FileAccess.Write, FileShare.None, descriptor.ChunkSize, useAsync: true))
        await using (var sourceStream = payload.File.OpenReadStream())
        {
            await sourceStream.CopyToAsync(targetStream, descriptor.ChunkSize, cancellationToken);
        }

        session.RegisterChunk(descriptor.ChunkIndex, chunkFilePath);
        RemoveExpiredSessions();
        return Result.Succeed();
    }

    /// <summary>
    /// Merges all chunks for a given upload and optionally commits the result to storage.
    /// </summary>
    /// <param name="request">Completion request.</param>
    /// <param name="storage">Storage abstraction used for committing the merged file.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The computed checksum and optional metadata when the file is committed.</returns>
    public async Task<Result<ChunkUploadCompleteResponse>> CompleteAsync(ChunkUploadCompleteRequest request, IStorage storage, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(storage);

        if (!_sessions.TryGetValue(request.UploadId, out var session))
        {
            return Result<ChunkUploadCompleteResponse>.Fail("Upload session not found");
        }

        try
        {
            session.EnsureAllChunksPresent();
            var orderedChunks = session.ChunkFiles
                .OrderBy(x => x.Key)
                .Select(x => x.Value)
                .ToArray();

            var mergedFilePath = Path.Combine(session.WorkingDirectory, session.FileName);
            await MergeChunksAsync(mergedFilePath, orderedChunks, cancellationToken);

            BlobMetadata? metadata = null;
            if (request.CommitToStorage)
            {
                var uploadOptions = new UploadOptions(request.FileName ?? session.FileName, request.Directory, request.ContentType, request.Metadata);
                var uploadResult = await storage.UploadAsync(new FileInfo(mergedFilePath), uploadOptions, cancellationToken);
                uploadResult.ThrowIfFail();
                metadata = uploadResult.Value;
            }

            var crc = Crc32Helper.CalculateFileCrc(mergedFilePath);

            if (!request.KeepMergedFile)
            {
                File.Delete(mergedFilePath);
            }

            session.Cleanup();
            _sessions.TryRemove(request.UploadId, out _);

            return Result<ChunkUploadCompleteResponse>.Succeed(new ChunkUploadCompleteResponse
            {
                Checksum = crc,
                Metadata = metadata
            });
        }
        catch (Exception ex)
        {
            return Result<ChunkUploadCompleteResponse>.Fail(ex);
        }
    }

    /// <summary>
    /// Aborts and cleans up the specified upload session.
    /// </summary>
    /// <param name="uploadId">Upload identifier.</param>
    public void Abort(string uploadId)
    {
        if (_sessions.TryRemove(uploadId, out var session))
        {
            session.Cleanup();
        }
    }

    private static string SanitizeUploadId(string uploadId)
    {
        if (string.IsNullOrWhiteSpace(uploadId))
            throw new ArgumentException("Upload ID cannot be null or empty", nameof(uploadId));

        // Remove any path traversal sequences
        uploadId = uploadId.Replace("..", string.Empty, StringComparison.Ordinal);
        uploadId = uploadId.Replace("/", string.Empty, StringComparison.Ordinal);
        uploadId = uploadId.Replace("\\", string.Empty, StringComparison.Ordinal);

        // Only allow alphanumeric characters, hyphens, and underscores
        var allowedChars = uploadId.Where(c => char.IsLetterOrDigit(c) || c == '-' || c == '_').ToArray();
        var sanitized = new string(allowedChars);

        if (string.IsNullOrWhiteSpace(sanitized))
            throw new ArgumentException("Upload ID contains only invalid characters", nameof(uploadId));

        if (sanitized.Length > 128)
            throw new ArgumentException("Upload ID is too long", nameof(uploadId));

        return sanitized;
    }

    private static async Task MergeChunksAsync(string destinationFile, IReadOnlyCollection<string> chunkFiles, CancellationToken cancellationToken)
    {
        await using var destination = new FileStream(destinationFile, FileMode.Create, FileAccess.Write, FileShare.None, bufferSize: MergeBufferSize, useAsync: true);

        foreach (var chunk in chunkFiles)
        {
            await using var source = new FileStream(chunk, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize: MergeBufferSize, useAsync: true);
            await source.CopyToAsync(destination, MergeBufferSize, cancellationToken);
        }
    }

    private void RemoveExpiredSessions()
    {
        if (_options.SessionTtl <= TimeSpan.Zero)
        {
            return;
        }

        var expirationThreshold = DateTimeOffset.UtcNow - _options.SessionTtl;
        foreach (var (uploadId, session) in _sessions)
        {
            if (session.LastTouchedUtc < expirationThreshold)
            {
                if (_sessions.TryRemove(uploadId, out var expired))
                {
                    expired.Cleanup();
                }
            }
        }
    }
}
