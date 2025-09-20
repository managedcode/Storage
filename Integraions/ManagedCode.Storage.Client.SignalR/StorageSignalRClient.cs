using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using ManagedCode.Storage.Client.SignalR.Models;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.AspNetCore.Http.Connections;

namespace ManagedCode.Storage.Client.SignalR;

/// <summary>
/// SignalR client capable of uploading and downloading content through the storage hub.
/// </summary>
public sealed class StorageSignalRClient : IStorageSignalRClient
{
    private readonly SemaphoreSlim _connectionLock = new(1, 1);
    private readonly List<IDisposable> _handlerRegistrations = new();

    private HubConnection? _connection;
    private StorageSignalRClientOptions? _options;
    private bool _disposed;

    /// <summary>
    /// Initialises a new instance of the SignalR storage client.
    /// </summary>
    public StorageSignalRClient()
    {
    }

    /// <summary>
    /// Initialises a new instance using the provided client options.
    /// </summary>
    /// <param name="options">Preconfigured client options.</param>
    public StorageSignalRClient(StorageSignalRClientOptions options)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    /// <inheritdoc />
    public event EventHandler<StorageTransferStatus>? TransferProgress;
    /// <inheritdoc />
    public event EventHandler<StorageTransferStatus>? TransferCompleted;
    /// <inheritdoc />
    public event EventHandler<StorageTransferStatus>? TransferCanceled;
    /// <inheritdoc />
    public event EventHandler<StorageTransferStatus>? TransferFaulted;

    /// <inheritdoc />
    public bool IsConnected => _connection?.State == HubConnectionState.Connected;

    /// <inheritdoc />
    public async Task ConnectAsync(StorageSignalRClientOptions options, CancellationToken cancellationToken = default)
    {
        if (options is null)
        {
            throw new ArgumentNullException(nameof(options));
        }

        await _connectionLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(StorageSignalRClient));
            }

            _options = options;

            if (IsConnected)
            {
                return;
            }

            _connection ??= BuildConnection(options);

            RegisterHubHandlers(_connection);

        if (options.KeepAliveInterval.HasValue)
        {
            _connection.KeepAliveInterval = options.KeepAliveInterval.Value;
        }

        if (options.ServerTimeout.HasValue)
        {
            _connection.ServerTimeout = options.ServerTimeout.Value;
        }

            await _connection.StartAsync(cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _connectionLock.Release();
        }
    }

    /// <inheritdoc />
    public Task ConnectAsync(CancellationToken cancellationToken = default)
    {
        if (_options is null)
        {
            throw new InvalidOperationException("ConnectAsync(StorageSignalRClientOptions) must be called before attempting parameterless connect.");
        }

        return ConnectAsync(_options, cancellationToken);
    }

    /// <inheritdoc />
    public async Task DisconnectAsync(CancellationToken cancellationToken = default)
    {
        await _connectionLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (_connection is null)
            {
                return;
            }

            if (_connection.State != HubConnectionState.Disconnected)
            {
                await _connection.StopAsync(cancellationToken).ConfigureAwait(false);
            }

            foreach (var handler in _handlerRegistrations)
            {
                handler.Dispose();
            }
            _handlerRegistrations.Clear();
        }
        finally
        {
            _connectionLock.Release();
        }
    }

    /// <inheritdoc />
    public async Task<StorageTransferStatus> UploadAsync(Stream stream, StorageUploadStreamDescriptor descriptor, IProgress<StorageTransferStatus>? progress = null, CancellationToken cancellationToken = default)
    {
        if (stream is null)
        {
            throw new ArgumentNullException(nameof(stream));
        }

        if (descriptor is null)
        {
            throw new ArgumentNullException(nameof(descriptor));
        }

        var connection = EnsureConnected();

        if (string.IsNullOrWhiteSpace(descriptor.FileName))
        {
            throw new ArgumentException("The upload descriptor must contain a file name.", nameof(descriptor));
        }

        descriptor.TransferId = string.IsNullOrWhiteSpace(descriptor.TransferId)
            ? Guid.NewGuid().ToString("N")
            : descriptor.TransferId;

        if (stream.CanSeek)
        {
            stream.Seek(0, SeekOrigin.Begin);
        }

        var handler = CreateProgressRelay(descriptor.TransferId, progress);

        var bufferSize = _options?.StreamBufferSize ?? 64 * 1024;
        if (bufferSize <= 0)
        {
            throw new InvalidOperationException("StreamBufferSize must be greater than zero.");
        }

        var channelCapacity = _options?.UploadChannelCapacity ?? 4;
        if (channelCapacity <= 0)
        {
            throw new InvalidOperationException("UploadChannelCapacity must be greater than zero.");
        }

        var channel = Channel.CreateBounded<byte[]>(new BoundedChannelOptions(channelCapacity)
        {
            SingleReader = true,
            SingleWriter = true,
            FullMode = BoundedChannelFullMode.Wait
        });

        Task producerTask = ProduceChunksAsync(stream, channel.Writer, bufferSize, cancellationToken);
        var completionSource = new TaskCompletionSource<StorageTransferStatus>(TaskCreationOptions.RunContinuationsAsynchronously);

        void OnCompleted(object? _, StorageTransferStatus status)
        {
            if (string.Equals(status.TransferId, descriptor.TransferId, StringComparison.OrdinalIgnoreCase))
            {
                completionSource.TrySetResult(status);
            }
        }

        void OnFaulted(object? _, StorageTransferStatus status)
        {
            if (string.Equals(status.TransferId, descriptor.TransferId, StringComparison.OrdinalIgnoreCase))
            {
                completionSource.TrySetException(new HubException(status.Error ?? "Upload failed."));
            }
        }

        void OnCanceled(object? _, StorageTransferStatus status)
        {
            if (string.Equals(status.TransferId, descriptor.TransferId, StringComparison.OrdinalIgnoreCase))
            {
                completionSource.TrySetCanceled(cancellationToken);
            }
        }

        TransferCompleted += OnCompleted;
        TransferFaulted += OnFaulted;
        TransferCanceled += OnCanceled;

        try
        {
            await connection.SendAsync("UploadStreamAsync", descriptor, channel.Reader, cancellationToken).ConfigureAwait(false);
            await producerTask.ConfigureAwait(false);
            return await completionSource.Task.WaitAsync(cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            TransferCompleted -= OnCompleted;
            TransferFaulted -= OnFaulted;
            TransferCanceled -= OnCanceled;
            handler?.Dispose();
        }
    }

    /// <inheritdoc />
    public async Task<StorageTransferStatus> DownloadAsync(string blobName, Stream destination, IProgress<StorageTransferStatus>? progress = null, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(blobName))
        {
            throw new ArgumentException("Blob name is required.", nameof(blobName));
        }

        if (destination is null)
        {
            throw new ArgumentNullException(nameof(destination));
        }

        var connection = EnsureConnected();

        StorageTransferStatus? lastStatus = null;
        using var handler = CreateDownloadProgressRelay(blobName, progress, status => lastStatus = status);

        var totalBytes = 0L;
        await foreach (var chunk in connection.StreamAsync<byte[]>("DownloadStreamAsync", blobName, cancellationToken).WithCancellation(cancellationToken))
        {
            await destination.WriteAsync(chunk, cancellationToken).ConfigureAwait(false);
            totalBytes += chunk.Length;
        }

        destination.Flush();

        return lastStatus ?? new StorageTransferStatus
        {
            Operation = "download",
            ResourceName = blobName,
            BytesTransferred = totalBytes,
            TotalBytes = totalBytes,
            IsCompleted = true
        };
    }

    /// <inheritdoc />
    public IAsyncEnumerable<byte[]> DownloadStreamAsync(string blobName, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(blobName))
        {
            throw new ArgumentException("Blob name is required.", nameof(blobName));
        }

        var connection = EnsureConnected();
        return connection.StreamAsync<byte[]>("DownloadStreamAsync", blobName, cancellationToken);
    }

    /// <inheritdoc />
    public Task<StorageTransferStatus?> GetStatusAsync(string transferId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(transferId))
        {
            throw new ArgumentException("Transfer id is required.", nameof(transferId));
        }

        var connection = EnsureConnected();
        return connection.InvokeAsync<StorageTransferStatus?>("GetStatusAsync", transferId, cancellationToken);
    }

    /// <inheritdoc />
    public Task CancelTransferAsync(string transferId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(transferId))
        {
            throw new ArgumentException("Transfer id is required.", nameof(transferId));
        }

        var connection = EnsureConnected();
        return connection.InvokeAsync("CancelTransferAsync", transferId, cancellationToken);
    }

    /// <summary>
    /// Disposes the client and associated hub connection resources.
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        await DisconnectAsync().ConfigureAwait(false);
        _connection?.DisposeAsync();
        _connection = null;
        _connectionLock.Dispose();
    }

    private HubConnection EnsureConnected()
    {
        if (_connection is null)
        {
            throw new InvalidOperationException("The client has not been connected. Call ConnectAsync first.");
        }

        if (_connection.State != HubConnectionState.Connected)
        {
            throw new InvalidOperationException("The SignalR hub connection is not active.");
        }

        return _connection;
    }

    private HubConnection BuildConnection(StorageSignalRClientOptions options)
    {
        var builder = new HubConnectionBuilder();

        builder.WithUrl(options.HubUrl.ToString(), httpOptions =>
        {
            if (options.HttpMessageHandlerFactory is not null)
            {
                httpOptions.HttpMessageHandlerFactory = _ => options.HttpMessageHandlerFactory.Invoke() ?? throw new InvalidOperationException("HttpMessageHandlerFactory returned null.");
            }

            if (options.TransportType.HasValue)
            {
                httpOptions.Transports = options.TransportType.Value;
            }

            if (options.AccessTokenProvider is not null)
            {
                httpOptions.AccessTokenProvider = () => options.AccessTokenProvider!(CancellationToken.None);
            }
        });

        builder.ConfigureLogging(logging =>
        {
            logging.AddConsole();
            logging.SetMinimumLevel(LogLevel.Debug);
        });

        if (options.EnableAutomaticReconnect)
        {
            if (options.ReconnectPolicy is not null)
            {
                builder.WithAutomaticReconnect(options.ReconnectPolicy);
            }
            else
            {
                builder.WithAutomaticReconnect();
            }
        }

        return builder.Build();
    }

    private void RegisterHubHandlers(HubConnection connection)
    {
        _handlerRegistrations.Add(connection.On<StorageTransferStatus>(StorageSignalREventNames.TransferProgress, status => TransferProgress?.Invoke(this, status)));
        _handlerRegistrations.Add(connection.On<StorageTransferStatus>(StorageSignalREventNames.TransferCompleted, status => TransferCompleted?.Invoke(this, status)));
        _handlerRegistrations.Add(connection.On<StorageTransferStatus>(StorageSignalREventNames.TransferCanceled, status => TransferCanceled?.Invoke(this, status)));
        _handlerRegistrations.Add(connection.On<StorageTransferStatus>(StorageSignalREventNames.TransferFaulted, status => TransferFaulted?.Invoke(this, status)));
    }

    private IDisposable? CreateProgressRelay(string transferId, IProgress<StorageTransferStatus>? progress)
    {
        if (progress is null)
        {
            return null;
        }

        EventHandler<StorageTransferStatus> handler = (_, status) =>
        {
            if (string.Equals(status.TransferId, transferId, StringComparison.OrdinalIgnoreCase))
            {
                progress.Report(status);
            }
        };

        TransferProgress += handler;
        TransferCompleted += handler;
        TransferCanceled += handler;
        TransferFaulted += handler;

        return new DelegateDisposable(() =>
        {
            TransferProgress -= handler;
            TransferCompleted -= handler;
            TransferCanceled -= handler;
            TransferFaulted -= handler;
        });
    }

    private IDisposable CreateDownloadProgressRelay(string blobName, IProgress<StorageTransferStatus>? progress, Action<StorageTransferStatus> assign)
    {
        EventHandler<StorageTransferStatus> handler = (_, status) =>
        {
            if (string.Equals(status.ResourceName, blobName, StringComparison.OrdinalIgnoreCase))
            {
                assign(status);
                progress?.Report(status);
            }
        };

        TransferProgress += handler;
        TransferCompleted += handler;
        TransferCanceled += handler;
        TransferFaulted += handler;

        return new DelegateDisposable(() =>
        {
            TransferProgress -= handler;
            TransferCompleted -= handler;
            TransferCanceled -= handler;
            TransferFaulted -= handler;
        });
    }

    private static async Task ProduceChunksAsync(Stream source, ChannelWriter<byte[]> writer, int bufferSize, CancellationToken cancellationToken)
    {
        var buffer = new byte[bufferSize];

        try
        {
            while (true)
            {
                var read = await source.ReadAsync(buffer.AsMemory(0, buffer.Length), cancellationToken).ConfigureAwait(false);
                if (read <= 0)
                {
                    break;
                }

                var chunk = buffer.Take(read).ToArray();
                await writer.WriteAsync(chunk, cancellationToken).ConfigureAwait(false);
            }
        }
        finally
        {
            writer.TryComplete();
        }
    }

    private sealed class DelegateDisposable : IDisposable
    {
        private readonly Action _dispose;
        private int _disposed;

        public DelegateDisposable(Action dispose)
        {
            _dispose = dispose;
        }

        public void Dispose()
        {
            if (Interlocked.Exchange(ref _disposed, 1) == 0)
            {
                _dispose();
            }
        }
    }
}
