using Microsoft.Extensions.DependencyInjection;

namespace ManagedCode.Storage.Server;

public static class StorageServiceCollectionExtensions
{
    public static IServiceCollection AddStorageSetupService(this IServiceCollection services)
    {
        return services.AddHostedService<StorageSetupBackgroundService>();
    }
}
