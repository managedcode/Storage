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
    private readonly ConcurrentDictionary<string, TransferRegistration> _transfers = new();

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
        foreach (var (_, registration) in _transfers)
        {
            if (registration.ConnectionId == Context.ConnectionId)
            {
                registration.Cancellation.Cancel();
            }
        }

        await base.OnDisconnectedAsync(exception);
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

        return Task.FromResult(_transfers.TryGetValue(transferId, out var registration) ? registration.Status : null);
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

        if (_transfers.TryGetValue(transferId, out var registration))
        {
            registration.Status.IsCanceled = true;
            registration.Cancellation.Cancel();
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Streams file content from the caller and commits the result to storage when complete.
    /// </summary>
    /// <param name="descriptor">Upload metadata.</param>
    /// <param name="stream">Chunked byte stream supplied by the caller.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The final transfer status including metadata.</returns>
    public virtual async Task UploadStreamAsync(UploadStreamDescriptor descriptor, ChannelReader<byte[]> stream, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(descriptor);
        ArgumentException.ThrowIfNullOrWhiteSpace(descriptor.FileName);

        Console.Error.WriteLine($"UploadStreamAsync invoked for {descriptor.FileName}");

        var transferId = string.IsNullOrWhiteSpace(descriptor.TransferId) ? Guid.NewGuid().ToString("N") : descriptor.TransferId!;
        var registration = RegisterTransfer(transferId, "upload", descriptor.FileName, descriptor.FileSize, cancellationToken);

        try
        {
            using var uploadCts = registration.Cancellation;
            var token = uploadCts.Token;
            var tempFilePath = Path.Combine(_options.TempPath, transferId + ".upload");

            await using (var tempStream = new FileStream(tempFilePath, FileMode.Create, FileAccess.Write, FileShare.None, _options.StreamBufferSize, useAsync: true))
            {
                while (await stream.WaitToReadAsync(token).ConfigureAwait(false))
                {
                    while (stream.TryRead(out var chunk))
                    {
                        token.ThrowIfCancellationRequested();

                        if (chunk is { Length: > 0 })
                        {
                            await tempStream.WriteAsync(chunk, token).ConfigureAwait(false);
                            registration.Status.BytesTransferred += chunk.Length;
                            registration.Touch();
                            await Clients.Caller.SendAsync(StorageHubEvents.TransferProgress, registration.Status, token).ConfigureAwait(false);
                        }
                    }
                }

                await tempStream.FlushAsync(token).ConfigureAwait(false);
            }

            if (registration.Status.IsCanceled)
            {
                throw new OperationCanceledException("Upload was canceled");
            }

            var uploadOptions = new UploadOptions(descriptor.FileName, descriptor.Directory, descriptor.ContentType, descriptor.Metadata);

            await using var sourceStream = new FileStream(tempFilePath, FileMode.Open, FileAccess.Read, FileShare.Read, _options.StreamBufferSize, useAsync: true);
            var result = await Storage.UploadAsync(sourceStream, uploadOptions, token);
            result.ThrowIfFail();

            registration.Status.Metadata = result.Value;
            registration.Status.IsCompleted = true;
            await Clients.Caller.SendAsync(StorageHubEvents.TransferCompleted, registration.Status, token);
            return;
        }
        catch (OperationCanceledException)
        {
            registration.Status.IsCanceled = true;
            registration.Status.Error ??= "Transfer canceled";
            await Clients.Caller.SendAsync(StorageHubEvents.TransferCanceled, registration.Status, CancellationToken.None);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "UploadStreamAsync failed for {TransferId}", transferId);
            registration.Status.Error = ex.ToString();
            await Clients.Caller.SendAsync(StorageHubEvents.TransferFaulted, registration.Status, CancellationToken.None);
            Console.Error.WriteLine($"UploadStreamAsync server error: {ex}");
            throw new HubException($"Upload failed: {ex}", ex);
        }
        finally
        {
            CleanupTransferFile(transferId);
            _transfers.TryRemove(transferId, out _);
        }
    }

    /// <summary>
    /// Streams file content from storage back to the connected client.
    /// </summary>
    /// <param name="blobName">Name of the blob/file to download.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>An async byte sequence that yields file chunks.</returns>
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

        var downloadResult = await Storage.GetStreamAsync(blobName, token);
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
                    read = await sourceStream.ReadAsync(buffer.AsMemory(0, _options.StreamBufferSize), token);
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

                try
                {
                    await Clients.Caller.SendAsync(StorageHubEvents.TransferProgress, registration.Status, token);
                }
                catch (OperationCanceledException oce)
                {
                    failure = oce;
                    wasCanceled = true;
                    throw;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "DownloadStreamAsync failed while reporting progress for {TransferId}", transferId);
                    failure = ex;
                    throw new HubException("Download failed", ex);
                }

                yield return chunk;
            }

            registration.Status.IsCompleted = true;
            try
            {
                await Clients.Caller.SendAsync(StorageHubEvents.TransferCompleted, registration.Status, token);
            }
            catch (OperationCanceledException oce)
            {
                failure = oce;
                wasCanceled = true;
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "DownloadStreamAsync failed while sending completion for {TransferId}", transferId);
                failure = ex;
                throw new HubException("Download failed", ex);
            }
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
            _transfers.TryRemove(transferId, out _);

            if (failure is not null)
            {
                if (wasCanceled)
                {
                    registration.Status.IsCanceled = true;
                    registration.Status.Error ??= "Transfer canceled";
                    _ = Clients.Caller.SendAsync(StorageHubEvents.TransferCanceled, registration.Status, CancellationToken.None);
                }
                else
                {
                    registration.Status.Error = failure.Message;
                    _ = Clients.Caller.SendAsync(StorageHubEvents.TransferFaulted, registration.Status, CancellationToken.None);
                }
            }
        }
    }

    private TransferRegistration RegisterTransfer(string transferId, string operation, string resourceName, long? totalBytes, CancellationToken cancellationToken)
    {
        if (_options.MaxConcurrentTransfers > 0 && _transfers.Count >= _options.MaxConcurrentTransfers)
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

        var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var registration = new TransferRegistration(status, cts, Context.ConnectionId);

        if (!_transfers.TryAdd(transferId, registration))
        {
            throw new HubException("Transfer identifier already exists");
        }

        return registration;
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
        public DateTimeOffset LastTouchedUtc { get; private set; }

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
