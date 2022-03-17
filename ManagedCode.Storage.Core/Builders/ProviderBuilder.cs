using Microsoft.Extensions.DependencyInjection;

namespace ManagedCode.Storage.Core.Builders;

public class ProviderBuilder
{
    public ProviderBuilder(IServiceCollection serviceCollection)
    {
        ServiceCollection = serviceCollection;
    }

    public IServiceCollection ServiceCollection { get; }
}