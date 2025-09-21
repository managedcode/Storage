using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using ManagedCode.Storage.Client.SignalR.Models;

namespace ManagedCode.Storage.Client.SignalR;

/// <summary>
/// Defines the contract for interacting with the storage SignalR hub.
/// </summary>
public interface IStorageSignalRClient : IAsyncDisposable
{
    /// <summary>
    /// Occurs when the hub reports transfer progress.
    /// </summary>
    event EventHandler<StorageTransferStatus>? TransferProgress;

    /// <summary>
    /// Occurs when the hub reports that a transfer has completed successfully.
    /// </summary>
    event EventHandler<StorageTransferStatus>? TransferCompleted;

    /// <summary>
    /// Occurs when the hub reports that a transfer was canceled.
    /// </summary>
    event EventHandler<StorageTransferStatus>? TransferCanceled;

    /// <summary>
    /// Occurs when the hub reports that a transfer has faulted.
    /// </summary>
    event EventHandler<StorageTransferStatus>? TransferFaulted;

    /// <summary>
    /// Gets a value indicating whether the client is currently connected to the hub.
    /// </summary>
    bool IsConnected { get; }

    /// <summary>
    /// Establishes a connection to the storage hub.
    /// </summary>
    /// <param name="options">Connection options.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task ConnectAsync(StorageSignalRClientOptions options, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gracefully disconnects from the storage hub.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task DisconnectAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Streams the provided content to the server and commits it to storage.
    /// </summary>
    /// <param name="stream">Input stream containing the payload.</param>
    /// <param name="descriptor">Upload descriptor metadata.</param>
    /// <param name="progress">Optional progress reporter receiving hub status updates.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<StorageTransferStatus> UploadAsync(Stream stream, StorageUploadStreamDescriptor descriptor, IProgress<StorageTransferStatus>? progress = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Streams a blob from storage directly into the provided destination stream.
    /// </summary>
    /// <param name="blobName">Name of the blob to download.</param>
    /// <param name="destination">Destination stream to receive the payload.</param>
    /// <param name="progress">Optional progress reporter receiving hub status updates.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<StorageTransferStatus> DownloadAsync(string blobName, Stream destination, IProgress<StorageTransferStatus>? progress = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Streams a blob from storage as an asynchronous byte sequence.
    /// </summary>
    /// <param name="blobName">Name of the blob to download.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>An async enumerable yielding chunks of the blob.</returns>
    IAsyncEnumerable<byte[]> DownloadStreamAsync(string blobName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves the current status of a transfer tracked by the hub.
    /// </summary>
    /// <param name="transferId">Transfer identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<StorageTransferStatus?> GetStatusAsync(string transferId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Requests cancellation of the specified transfer.
    /// </summary>
    /// <param name="transferId">Transfer identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task CancelTransferAsync(string transferId, CancellationToken cancellationToken = default);
}
