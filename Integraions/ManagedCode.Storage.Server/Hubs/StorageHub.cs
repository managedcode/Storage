using ManagedCode.Storage.Core;
using Microsoft.Extensions.Logging;

namespace ManagedCode.Storage.Server.Hubs;

/// <summary>
/// Default hub implementation that proxies operations to the shared <see cref="IStorage"/> instance.
/// </summary>
public class StorageHub : StorageHubBase<IStorage>
{
    /// <summary>
    /// Initialises a new instance of the storage hub.
    /// </summary>
    /// <param name="storage">The storage instance hosted by the application.</param>
    /// <param name="options">Hub options.</param>
    /// <param name="logger">Logger.</param>
    public StorageHub(IStorage storage, StorageHubOptions options, ILogger<StorageHub> logger)
        : base(storage, options, logger)
    {
    }
}
