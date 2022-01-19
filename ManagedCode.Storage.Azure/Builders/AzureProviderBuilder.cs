using ManagedCode.Storage.Core.Builders;
using Microsoft.Extensions.DependencyInjection;

namespace ManagedCode.Storage.Azure.Builders
{
    public class AzureProviderBuilder : ProviderBuilder
    {
        public AzureProviderBuilder(IServiceCollection serviceCollection) : base(serviceCollection) {}


    }
}
