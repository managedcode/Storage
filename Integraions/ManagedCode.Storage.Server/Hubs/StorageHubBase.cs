using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using ManagedCode.Communication;
using ManagedCode.Storage.Core;
using ManagedCode.Storage.Core.Models;
using ManagedCode.Storage.Server.Models;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace ManagedCode.Storage.Server.Hubs;

/// <summary>
/// Base SignalR hub exposing upload and download streaming operations backed by an <see cref="IStorage"/> implementation.
/// </summary>
/// <typeparam name="TStorage">Concrete storage type.</typeparam>
public abstract class StorageHubBase<TStorage> : Hub where TStorage : IStorage
{
    private readonly ILogger _logger;
    private readonly StorageHubOptions _options;
    private static readonly StorageHubTransferRegistry Transfers = new();

    /// <summary>
    /// Initialises a new hub instance.
    /// </summary>
    /// <param name="storage">Backing storage provider.</param>
    /// <param name="options">Runtime options for streaming.</param>
    /// <param name="logger">Logger used for diagnostic output.</param>
    protected StorageHubBase(TStorage storage, StorageHubOptions options, ILogger logger)
    {
        Storage = storage ?? throw new ArgumentNullException(nameof(storage));
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        Directory.CreateDirectory(_options.TempPath);
    }

    /// <summary>
    /// Gets the storage provider backing the hub operations.
    /// </summary>
    protected TStorage Storage { get; }

    /// <inheritdoc />
    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        Transfers.CancelConnection(Context.ConnectionId);
        await base.OnDisconnectedAsync(exception).ConfigureAwait(false);
    }

    /// <summary>
    /// Retrieves the status for a known transfer, if present.
    /// </summary>
    /// <param name="transferId">Transfer identifier.</param>
    /// <returns>The latest status or <c>null</c> if unknown.</returns>
    public virtual Task<TransferStatus?> GetStatusAsync(string transferId)
    {
        if (string.IsNullOrWhiteSpace(transferId))
        {
            return Task.FromResult<TransferStatus?>(null);
        }

        var registration = Transfers.Find(Context.ConnectionId, transferId);
        return Task.FromResult(registration is null ? null : CreateStatusSnapshot(registration.Status));
    }

    /// <summary>
    /// Requests cancellation of the specified transfer.
    /// </summary>
    /// <param name="transferId">Transfer identifier.</param>
    /// <returns>A task representing the async operation.</returns>
    public virtual Task CancelTransferAsync(string transferId)
    {
        if (string.IsNullOrWhiteSpace(transferId))
        {
            return Task.CompletedTask;
        }

        Transfers.Cancel(Context.ConnectionId, transferId);

        return Task.CompletedTask;
    }

    /// <summary>
    /// Begins an upload by registering metadata and reserving a transfer identifier.
    /// </summary>
    /// <param name="descriptor">Upload metadata.</param>
    /// <returns>The transfer identifier that must be used for the content stream.</returns>
    public virtual Task<string> BeginUploadStreamAsync(UploadStreamDescriptor descriptor)
    {
        ArgumentNullException.ThrowIfNull(descriptor);
        ArgumentException.ThrowIfNullOrWhiteSpace(descriptor.FileName);
        if (descriptor.FileSize < 0)
            throw new HubException("Declared file size cannot be negative.");

        var transferId = string.IsNullOrWhiteSpace(descriptor.TransferId)
            ? Guid.NewGuid().ToString("N")
            : descriptor.TransferId!;

        Transfers.Register(Context, _options.MaxConcurrentTransfers, transferId, "upload", descriptor.FileName,
            descriptor.FileSize, CancellationToken.None, descriptor);

        _logger.LogInformation("BeginUploadStreamAsync registered {FileName} with TransferId {TransferId}", descriptor.FileName, transferId);

        return Task.FromResult(transferId);
    }

    /// <summary>
    /// Streams file content from the caller and commits the result to storage when complete.
    /// </summary>
    /// <param name="transferId">The transfer identifier previously returned by <see cref="BeginUploadStreamAsync"/>.</param>
    /// <param name="stream">Chunked byte stream supplied by the caller.</param>
    /// <param name="cancellationToken">Cancellation token that aborts the upload stream.</param>
    /// <returns>A channel producing transfer status updates as the upload progresses.</returns>
    public virtual async IAsyncEnumerable<TransferStatus> UploadStreamContentAsync(
        string transferId,
        IAsyncEnumerable<byte[]> stream,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(transferId))
        {
            throw new HubException("Transfer identifier is required");
        }

        var registration = Transfers.Find(Context.ConnectionId, transferId);
        if (registration is null)
        {
            throw new HubException($"Unknown transfer id '{transferId}'");
        }

        if (!string.Equals(registration.Status.Operation, "upload", StringComparison.OrdinalIgnoreCase))
        {
            throw new HubException($"Transfer '{transferId}' is not registered for upload.");
        }

        if (!registration.TryStartUpload())
        {
            throw new HubException($"Upload for transfer '{transferId}' has already started.");
        }

        var descriptor = registration.UploadDescriptor ?? throw new HubException($"Transfer '{registration.Status.TransferId}' is missing an upload descriptor.");
        var transferIdValue = registration.Status.TransferId;
        var tempFilePath = Path.Combine(_options.TempPath, Guid.NewGuid().ToString("N") + ".upload");

        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(registration.Cancellation.Token, cancellationToken);
        var token = linkedCts.Token;
        var completionEmitted = false;

        try
        {
            await using (var tempStream = new FileStream(tempFilePath, FileMode.CreateNew, FileAccess.Write, FileShare.None, _options.StreamBufferSize, useAsync: true))
            {
                await foreach (var chunk in stream.WithCancellation(token).ConfigureAwait(false))
                {
                    if (chunk is not { Length: > 0 })
                    {
                        continue;
                    }

                    if (descriptor.FileSize is long expected && chunk.LongLength > expected - registration.Status.BytesTransferred)
                    {
                        registration.Status.Error = "Upload exceeds its declared length";
                        throw new HubException(registration.Status.Error);
                    }

                    await tempStream.WriteAsync(chunk, token).ConfigureAwait(false);
                    registration.Status.BytesTransferred += chunk.Length;
                    registration.Touch();

                    var progressSnapshot = CreateStatusSnapshot(registration.Status);
                    await NotifyClientAsync(StorageHubEvents.TransferProgress, progressSnapshot, token).ConfigureAwait(false);
                    yield return progressSnapshot;
                }

                await tempStream.FlushAsync(token).ConfigureAwait(false);
            }

            if (registration.Status.IsCanceled)
            {
                registration.Status.Error ??= "Transfer canceled";
                var canceledSnapshot = CreateStatusSnapshot(registration.Status);
                await NotifyClientAsync(StorageHubEvents.TransferCanceled, canceledSnapshot, CancellationToken.None).ConfigureAwait(false);
                yield break;
            }

            if (descriptor.FileSize is long length && length != registration.Status.BytesTransferred)
            {
                registration.Status.Error = "Upload is shorter than its declared length";
                throw new HubException(registration.Status.Error);
            }

            await using (var sourceStream = new FileStream(tempFilePath, FileMode.Open, FileAccess.Read, FileShare.Read, _options.StreamBufferSize, useAsync: true))
            {
                var uploadOptions = new UploadOptions(descriptor.FileName, descriptor.Directory, descriptor.ContentType, descriptor.Metadata);
                var result = await Storage.UploadAsync(sourceStream, uploadOptions, token).ConfigureAwait(false);
                result.ThrowIfFail();
                registration.Status.Metadata = result.Value;
            }

            registration.Status.IsCompleted = true;
            var completionSnapshot = CreateStatusSnapshot(registration.Status);
            await NotifyClientAsync(StorageHubEvents.TransferCompleted, completionSnapshot, token).ConfigureAwait(false);
            completionEmitted = true;
            yield return completionSnapshot;
        }
        finally
        {
            CleanupTransferFile(tempFilePath, transferIdValue);
            Transfers.Remove(registration);

            if (!completionEmitted)
            {
                if (registration.Status.IsCanceled)
                {
                    registration.Status.Error ??= "Transfer canceled";
                    _ = NotifyClientAsync(StorageHubEvents.TransferCanceled, CreateStatusSnapshot(registration.Status), CancellationToken.None);
                }
                else if (registration.Status.Error is not null)
                {
                    _ = NotifyClientAsync(StorageHubEvents.TransferFaulted, CreateStatusSnapshot(registration.Status), CancellationToken.None);
                }
            }
        }
    }

    private async Task NotifyClientAsync(string eventName, TransferStatus snapshot, CancellationToken cancellationToken)
    {
        try
        {
            await Clients.Caller.SendAsync(eventName, snapshot, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            // Caller disconnected or canceled. Nothing else to do.
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Failed to emit {EventName} for transfer {TransferId}", eventName, snapshot.TransferId);
        }
    }

    public virtual async IAsyncEnumerable<byte[]> DownloadStreamAsync(string blobName, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(blobName);

        var transferId = Guid.NewGuid().ToString("N");
        var registration = Transfers.Register(Context, _options.MaxConcurrentTransfers, transferId, "download",
            blobName, null, cancellationToken);
        byte[]? buffer = null;
        Exception? failure = null;
        var wasCanceled = false;

        var token = registration.Cancellation.Token;

        try
        {
            await using var sourceStream = await OpenDownloadStreamAsync(blobName, token).ConfigureAwait(false);
            registration.Status.TotalBytes = sourceStream.CanSeek ? sourceStream.Length : null;
            buffer = ArrayPool<byte>.Shared.Rent(_options.StreamBufferSize);

            while (true)
            {
                int read;
                try
                {
                    read = await sourceStream.ReadAsync(buffer.AsMemory(0, _options.StreamBufferSize), token).ConfigureAwait(false);
                }
                catch (OperationCanceledException oce)
                {
                    failure = oce;
                    wasCanceled = true;
                    throw;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "DownloadStreamAsync failed while reading for {TransferId}", transferId);
                    failure = ex;
                    throw new HubException("Download failed", ex);
                }

                if (read == 0)
                {
                    break;
                }

                var chunk = new byte[read];
                Array.Copy(buffer, 0, chunk, 0, read);
                registration.Status.BytesTransferred += read;
                registration.Touch();

                var progressSnapshot = CreateStatusSnapshot(registration.Status);
                try
                {
                    await NotifyClientAsync(StorageHubEvents.TransferProgress, progressSnapshot, token).ConfigureAwait(false);
                }
                catch (OperationCanceledException oce)
                {
                    failure = oce;
                    wasCanceled = true;
                    throw;
                }
                catch (HubException)
                {
                    throw;
                }
                yield return chunk;
            }

            registration.Status.IsCompleted = true;
            var completionSnapshot = CreateStatusSnapshot(registration.Status);
            try
            {
                await NotifyClientAsync(StorageHubEvents.TransferCompleted, completionSnapshot, token).ConfigureAwait(false);
            }
            catch (OperationCanceledException oce)
            {
                failure = oce;
                wasCanceled = true;
                throw;
            }
        }
        finally
        {
            if (buffer is not null)
                ArrayPool<byte>.Shared.Return(buffer);
            Transfers.Remove(registration);

            if (failure is not null)
            {
                if (wasCanceled)
                {
                    registration.Status.IsCanceled = true;
                    registration.Status.Error ??= "Transfer canceled";
                    _ = NotifyClientAsync(StorageHubEvents.TransferCanceled, CreateStatusSnapshot(registration.Status), CancellationToken.None);
                }
                else
                {
                    registration.Status.Error = failure.Message;
                    _ = NotifyClientAsync(StorageHubEvents.TransferFaulted, CreateStatusSnapshot(registration.Status), CancellationToken.None);
                }
            }
        }
    }

    private async Task<Stream> OpenDownloadStreamAsync(string blobName, CancellationToken cancellationToken)
    {
        var result = await Storage.GetStreamAsync(blobName, cancellationToken).ConfigureAwait(false);
        result.ThrowIfFail();
        return result.Value ?? throw new HubException("Storage returned an empty download stream.");
    }

    private static TransferStatus CreateStatusSnapshot(TransferStatus status)
    {
        return new TransferStatus
        {
            TransferId = status.TransferId,
            Operation = status.Operation,
            ResourceName = status.ResourceName,
            BytesTransferred = status.BytesTransferred,
            TotalBytes = status.TotalBytes,
            IsCompleted = status.IsCompleted,
            IsCanceled = status.IsCanceled,
            Error = status.Error,
            Metadata = status.Metadata
        };
    }

    private void CleanupTransferFile(string tempFilePath, string transferId)
    {
        try
        {
            if (File.Exists(tempFilePath))
            {
                File.Delete(tempFilePath);
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Failed to clean up temp file for transfer {TransferId}", transferId);
        }
    }


}

/// <summary>
/// Event names emitted by <see cref="StorageHubBase{TStorage}"/>.
/// </summary>
public static class StorageHubEvents
{
    public const string TransferProgress = "TransferProgress";
    public const string TransferCompleted = "TransferCompleted";
    public const string TransferCanceled = "TransferCanceled";
    public const string TransferFaulted = "TransferFaulted";
}
