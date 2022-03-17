using ManagedCode.Storage.Core.Builders;
using Microsoft.Extensions.DependencyInjection;

namespace ManagedCode.Storage.Core.Extensions;

public static class ServiceCollectionExtensions
{
    public static ProviderBuilder AddManagedCodeStorage(this IServiceCollection serviceCollection)
    {
        return new ProviderBuilder(serviceCollection);
    }
}