using ManagedCode.Storage.Core.Providers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace ManagedCode.Storage.Core.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddStorageFactory(this IServiceCollection serviceCollection)
    {
        serviceCollection.TryAddSingleton<IStorageFactory, StorageFactory>();
        return serviceCollection;
    }

}