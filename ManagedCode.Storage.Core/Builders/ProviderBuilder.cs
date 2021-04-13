using Microsoft.Extensions.DependencyInjection;

namespace ManagedCode.Storage.Core.Builders
{
    public class ProviderBuilder
    {
        public IServiceCollection ServiceCollection { get; }

        public ProviderBuilder(IServiceCollection serviceCollection)
        {
            ServiceCollection = serviceCollection;
        }
    }
}
