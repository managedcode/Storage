using System;

namespace ManagedCode.Storage.Server.Hubs;

/// <summary>
/// Configures runtime behaviour of the storage SignalR hub.
/// </summary>
public class StorageHubOptions
{
    /// <summary>
    /// Temporary folder where incoming SignalR uploads are staged before being committed to storage.
    /// </summary>
    public string TempPath { get; set; } = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "managedcode-storage-hub");

    /// <summary>
    /// Size of the buffer used when streaming data to and from storage.
    /// </summary>
    public int StreamBufferSize { get; set; } = 64 * 1024;

    /// <summary>
    /// Maximum number of simultaneous streaming transfers per hub instance. Zero or negative disables the limit.
    /// </summary>
    public int MaxConcurrentTransfers { get; set; } = 0;

    /// <summary>
    /// Gets or sets the timeout after which idle transfers are canceled.
    /// </summary>
    public TimeSpan IdleTimeout { get; set; } = TimeSpan.FromMinutes(10);
}
