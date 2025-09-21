using System;
using System.Buffers;
using System.Collections.Concurrent;
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
    private static readonly ConcurrentDictionary<string, TransferRegistration> Transfers = new();

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
        foreach (var (_, registration) in Transfers)
        {
            if (registration.ConnectionId == Context.ConnectionId)
            {
                registration.Cancellation.Cancel();
            }
        }

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

        return Task.FromResult(Transfers.TryGetValue(transferId, out var registration)
            ? CreateStatusSnapshot(registration.Status)
            : null);
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

        if (Transfers.TryGetValue(transferId, out var registration))
        {
            registration.Status.IsCanceled = true;
            registration.Cancellation.Cancel();
        }

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

        var transferId = string.IsNullOrWhiteSpace(descriptor.TransferId)
            ? Guid.NewGuid().ToString("N")
            : descriptor.TransferId!;

        var registration = RegisterTransfer(transferId, "upload", descriptor.FileName, descriptor.FileSize, CancellationToken.None);
        registration.UploadDescriptor = descriptor;
        registration.Status.TotalBytes = descriptor.FileSize;

        _logger.LogInformation("BeginUploadStreamAsync registered {FileName} with TransferId {TransferId}", descriptor.FileName, transferId);

        return Task.FromResult(transferId);
    }

    /// <summary>
    /// Streams file content from the caller and commits the result to storage when complete.
    /// </summary>
    /// <param name="transferId">The transfer identifier previously returned by <see cref="BeginUploadStreamAsync"/>.</param>
    /// <param name="stream">Chunked byte stream supplied by the caller.</param>
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

        if (!Transfers.TryGetValue(transferId, out var registration))
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
        var tempFilePath = Path.Combine(_options.TempPath, transferIdValue + ".upload");
        registration.TempFilePath = tempFilePath;

        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(registration.Cancellation.Token, cancellationToken);
        var token = linkedCts.Token;
        var completionEmitted = false;

        try
        {
            await using (var tempStream = new FileStream(tempFilePath, FileMode.Create, FileAccess.Write, FileShare.None, _options.StreamBufferSize, useAsync: true))
            {
                await foreach (var chunk in stream.WithCancellation(token).ConfigureAwait(false))
                {
                    if (chunk is not { Length: > 0 })
                    {
                        continue;
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
            CleanupTransferFile(transferIdValue);
            Transfers.TryRemove(transferIdValue, out _);

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
        var registration = RegisterTransfer(transferId, "download", blobName, null, cancellationToken);
        var buffer = ArrayPool<byte>.Shared.Rent(_options.StreamBufferSize);
        Exception? failure = null;
        var wasCanceled = false;

        using var downloadCts = registration.Cancellation;
        var token = downloadCts.Token;

        var downloadResult = await Storage.GetStreamAsync(blobName, token).ConfigureAwait(false);
        downloadResult.ThrowIfFail();

        var sourceStreamResult = downloadResult.Value ?? throw new HubException("Download failed", new InvalidOperationException("Storage returned empty stream."));
        registration.Status.TotalBytes = sourceStreamResult.CanSeek ? sourceStreamResult.Length : null;

        await using var sourceStream = sourceStreamResult;

        try
        {
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
            ArrayPool<byte>.Shared.Return(buffer);
            Transfers.TryRemove(transferId, out _);

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

    private TransferRegistration RegisterTransfer(string transferId, string operation, string resourceName, long? totalBytes, CancellationToken cancellationToken)
    {
        if (_options.MaxConcurrentTransfers > 0 && Transfers.Count >= _options.MaxConcurrentTransfers)
        {
            throw new HubException("Too many concurrent transfers");
        }

        var status = new TransferStatus
        {
            TransferId = transferId,
            Operation = operation,
            ResourceName = resourceName,
            TotalBytes = totalBytes
        };

        var cts = CancellationTokenSource.CreateLinkedTokenSource(Context.ConnectionAborted, cancellationToken);
        var registration = new TransferRegistration(status, cts, Context.ConnectionId);

        if (!Transfers.TryAdd(transferId, registration))
        {
            throw new HubException("Transfer identifier already exists");
        }

        return registration;
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

    private void CleanupTransferFile(string transferId)
    {
        try
        {
            var tempFilePath = Path.Combine(_options.TempPath, transferId + ".upload");
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

    private sealed class TransferRegistration
    {
        private int _uploadStarted;

        public TransferRegistration(TransferStatus status, CancellationTokenSource cancellation, string connectionId)
        {
            Status = status;
            Cancellation = cancellation;
            ConnectionId = connectionId;
            LastTouchedUtc = DateTimeOffset.UtcNow;
        }

        public TransferStatus Status { get; }
        public CancellationTokenSource Cancellation { get; }
        public string ConnectionId { get; }
        public UploadStreamDescriptor? UploadDescriptor { get; set; }
        public string? TempFilePath { get; set; }
        public DateTimeOffset LastTouchedUtc { get; private set; }

        public bool TryStartUpload()
        {
            return Interlocked.Exchange(ref _uploadStarted, 1) == 0;
        }

        public void Touch()
        {
            LastTouchedUtc = DateTimeOffset.UtcNow;
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
