using Microsoft.Extensions.DependencyInjection;

namespace ManagedCode.Storage.Server.Extensions.DependencyInjection;

public static class StorageServiceCollectionExtensions
{
    public static IServiceCollection AddStorageSetupService(this IServiceCollection services)
    {
        return services.AddHostedService<StorageSetupBackgroundService>();
    }
}
