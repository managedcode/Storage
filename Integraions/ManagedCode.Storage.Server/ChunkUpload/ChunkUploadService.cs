using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ManagedCode.Communication;
using ManagedCode.Storage.Core;
using ManagedCode.Storage.Core.Models;
using ManagedCode.Storage.Server.Models;

namespace ManagedCode.Storage.Server.ChunkUpload;

/// <summary>
/// Coordinates multi-part uploads by persisting temporary chunks and merging them on completion.
/// </summary>
public sealed class ChunkUploadService
{
    private readonly ChunkUploadOptions _options;
    private readonly ConcurrentDictionary<string, ChunkUploadSession> _sessions = new();
    private readonly object _admission = new();

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
        cancellationToken.ThrowIfCancellationRequested();

        var descriptor = payload.Payload;
        var uploadId = ValidateUploadId(ChunkUploadDescriptor.ResolveUploadId(descriptor));
        if (descriptor.ChunkIndex <= 0 || descriptor.TotalChunks < 0 ||
            (descriptor.TotalChunks > 0 && descriptor.ChunkIndex > descriptor.TotalChunks) ||
            descriptor.ChunkSize <= 0 || descriptor.FileSize < 0 || payload.File.Length > descriptor.ChunkSize)
            return Result.Fail(HttpStatusCode.BadRequest, "Invalid chunk index or declared size");

        RemoveExpiredSessions();
        var session = AdmitSession(uploadId, descriptor);
        if (session is null)
            return Result.Fail("Maximum number of parallel chunk uploads exceeded");

        await session.Gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (session.Closed)
                return Result.Fail("Upload session is closed");
            if (session.TotalChunks != descriptor.TotalChunks || session.FileSize != descriptor.FileSize)
                return Result.Fail(HttpStatusCode.BadRequest, "Chunk does not match the upload session");

            await ChunkUploadFiles.StageChunkAsync(session, payload, cancellationToken).ConfigureAwait(false);
            return Result.Succeed();
        }
        finally
        {
            session.Gate.Release();
        }
    }

    private ChunkUploadSession? AdmitSession(string uploadId, FilePayload descriptor)
    {
        lock (_admission)
        {
            if (_sessions.TryGetValue(uploadId, out var existing))
                return existing;
            if (_options.MaxActiveSessions > 0 && _sessions.Count >= _options.MaxActiveSessions)
                return null;

            var directoryKey = Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(uploadId)));
            var directory = Path.Combine(_options.TempPath, directoryKey);
            Directory.CreateDirectory(directory);
            var session = new ChunkUploadSession(uploadId, descriptor.FileName ?? descriptor.UploadId,
                descriptor.ContentType, descriptor.TotalChunks, descriptor.ChunkSize, descriptor.FileSize, directory);
            _sessions[uploadId] = session;
            return session;
        }
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

        var acquired = false;
        try
        {
            await session.Gate.WaitAsync(cancellationToken).ConfigureAwait(false);
            acquired = true;
            if (session.Closed)
                return Result<ChunkUploadCompleteResponse>.Fail("Upload session is closed");

            session.Touch();
            session.EnsureAllChunksPresent();
            var orderedChunks = session.ChunkFiles
                .OrderBy(x => x.Key)
                .Select(x => x.Value)
                .ToArray();

            var mergedFilePath = Path.Combine(session.WorkingDirectory, "merged.bin");
            var expectedLength = session.TotalChunks == 0 && session.FileSize == 0 ? null : session.FileSize;
            var crc = await ChunkUploadFiles.MergeChunksAsync(mergedFilePath, orderedChunks, expectedLength, cancellationToken).ConfigureAwait(false);

            BlobMetadata? metadata = null;
            if (request.CommitToStorage)
            {
                var uploadOptions = new UploadOptions(request.FileName ?? session.FileName, request.Directory, request.ContentType, request.Metadata);
                var uploadResult = await storage.UploadAsync(new FileInfo(mergedFilePath), uploadOptions, cancellationToken)
                    .ConfigureAwait(false);
                uploadResult.ThrowIfFail();
                metadata = uploadResult.Value;
            }

            CloseSession(session, request.KeepMergedFile);

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
        finally
        {
            if (acquired)
                session.Gate.Release();
        }
    }

    /// <summary>
    /// Aborts and cleans up the specified upload session.
    /// </summary>
    /// <param name="uploadId">Upload identifier.</param>
    public void Abort(string uploadId)
    {
        if (!_sessions.TryGetValue(uploadId, out var session))
            return;

        session.Gate.Wait();
        try
        {
            if (!session.Closed)
                CloseSession(session);
        }
        finally
        {
            session.Gate.Release();
        }
    }

    private void CloseSession(ChunkUploadSession session, bool keepMergedFile = false)
    {
        session.Closed = true;
        try
        {
            session.Cleanup(keepMergedFile);
        }
        finally
        {
            _sessions.TryRemove(session.UploadId, out _);
        }
    }

    private static string ValidateUploadId(string uploadId)
    {
        if (string.IsNullOrWhiteSpace(uploadId))
            throw new ArgumentException("Upload ID cannot be null or empty", nameof(uploadId));

        if (uploadId.Length > 128 || uploadId.Any(c => !char.IsAsciiLetterOrDigit(c) && c is not '-' and not '_'))
            throw new ArgumentException("Upload ID must contain at most 128 ASCII letters, digits, hyphens or underscores", nameof(uploadId));

        return uploadId;
    }

    private void RemoveExpiredSessions()
    {
        if (_options.SessionTtl <= TimeSpan.Zero)
        {
            return;
        }

        var expirationThreshold = DateTimeOffset.UtcNow - _options.SessionTtl;
        foreach (var (_, session) in _sessions)
        {
            if (!session.Gate.Wait(0))
                continue;

            try
            {
                if (!session.Closed && session.LastTouchedUtc < expirationThreshold)
                    CloseSession(session);
            }
            finally
            {
                session.Gate.Release();
            }
        }
    }
}
