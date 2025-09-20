using ManagedCode.Storage.Core;
using Microsoft.Extensions.Logging;

namespace ManagedCode.Storage.Server.Hubs;

public class StorageHub : StorageHubBase<IStorage>
{
    public StorageHub(IStorage storage, StorageHubOptions options, ILogger<StorageHub> logger)
        : base(storage, options, logger)
    {
    }
}
