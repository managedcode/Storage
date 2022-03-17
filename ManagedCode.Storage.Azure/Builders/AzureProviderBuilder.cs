using System;
using ManagedCode.Storage.Azure.Options;
using ManagedCode.Storage.Core;
using ManagedCode.Storage.Core.Builders;
using ManagedCode.Storage.Core.Helpers;
using Microsoft.Extensions.DependencyInjection;

namespace ManagedCode.Storage.Azure.Builders;

public class AzureProviderBuilder : ProviderBuilder
{
    public AzureProviderBuilder(
        IServiceCollection serviceCollection,
        string connectionString) : base(serviceCollection)
    {
        _connectionString = connectionString;
    }

    private string _connectionString { get; }

    public AzureProviderBuilder Add<TAzureStorage>(Action<ContainerOptions> action)
        where TAzureStorage : IStorage
    {
        var containerOptions = new ContainerOptions();
        action.Invoke(containerOptions);

        var storageOptions = new StorageOptions
        {
            ConnectionString = _connectionString,
            Container = containerOptions.Container
        };

        var implementationType = TypeHelpers.GetImplementationType<TAzureStorage, AzureStorage, StorageOptions>();
        ServiceCollection.AddScoped(typeof(TAzureStorage), x => Activator.CreateInstance(implementationType, storageOptions));

        return this;
    }
}