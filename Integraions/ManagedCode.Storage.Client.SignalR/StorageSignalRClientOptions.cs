using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.SignalR.Client;

namespace ManagedCode.Storage.Client.SignalR;

/// <summary>
/// Represents configuration used by <see cref="StorageSignalRClient"/> when establishing a SignalR connection.
/// </summary>
public sealed class StorageSignalRClientOptions
{
    private Uri? _hubUrl;

    /// <summary>
    /// Gets or sets the absolute hub URL. This value is required.
    /// </summary>
    public Uri HubUrl
    {
        get => _hubUrl ?? throw new InvalidOperationException("HubUrl has not been configured.");
        set => _hubUrl = value ?? throw new ArgumentNullException(nameof(value));
    }

    /// <summary>
    /// Gets or sets a delegate that provides an access token for authenticated hubs.
    /// </summary>
    public Func<CancellationToken, Task<string?>>? AccessTokenProvider { get; set; }

    /// <summary>
    /// Gets or sets a factory providing the <see cref="HttpMessageHandler"/> used by the SignalR client.
    /// </summary>
    public Func<HttpMessageHandler?>? HttpMessageHandlerFactory { get; set; }

    /// <summary>
    /// Gets or sets the preferred transport. When <c>null</c> the default transport negotiation is used.
    /// </summary>
    public HttpTransportType? TransportType { get; set; }

    /// <summary>
    /// Gets or sets the custom reconnect policy. If null and <see cref="EnableAutomaticReconnect"/> is true, the default reconnect policy is used.
    /// </summary>
    public IRetryPolicy? ReconnectPolicy { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether automatic reconnect is enabled.
    /// </summary>
    public bool EnableAutomaticReconnect { get; set; } = true;

    /// <summary>
    /// Gets or sets the keep-alive interval applied to the SignalR connection.
    /// </summary>
    public TimeSpan? KeepAliveInterval { get; set; }

    /// <summary>
    /// Gets or sets the server timeout applied to the SignalR connection.
    /// </summary>
    public TimeSpan? ServerTimeout { get; set; }

    /// <summary>
    /// Gets or sets the buffer size used when streaming uploads.
    /// </summary>
    public int StreamBufferSize { get; set; } = 64 * 1024;

    /// <summary>
    /// Gets or sets the bounded channel capacity used for upload streaming.
    /// </summary>
    public int UploadChannelCapacity { get; set; } = 4;
}
