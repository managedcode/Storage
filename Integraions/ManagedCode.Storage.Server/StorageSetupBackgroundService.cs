using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ManagedCode.Storage.Core;
using Microsoft.Extensions.Hosting;

namespace ManagedCode.Storage.Server;

public class StorageSetupBackgroundService(IEnumerable<IStorage> storages) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        foreach (var storage in storages)
            await storage.CreateContainerAsync(stoppingToken);
    }
}