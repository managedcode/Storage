using Microsoft.Extensions.DependencyInjection;
using ManagedCode.Storage.Core.Builders;

namespace ManagedCode.Storage.Core.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static ProviderBuilder AddManagedCodeStorage(this IServiceCollection serviceCollection)
        {
            return new ProviderBuilder(serviceCollection);
        }
    }
}
