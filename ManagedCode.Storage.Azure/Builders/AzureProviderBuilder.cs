using System;
using Microsoft.Extensions.DependencyInjection;
using ManagedCode.Storage.Azure.Options;
using ManagedCode.Storage.Core.Builders;
using ManagedCode.Storage.Core.Helpers;
using ManagedCode.Storage.Core;

namespace ManagedCode.Storage.Azure.Builders
{
    public class AzureProviderBuilder : ProviderBuilder
    {
        private string _connectionString { get; set; }

        public AzureProviderBuilder(
            IServiceCollection serviceCollection, 
            string connectionString) : base(serviceCollection) 
        {
            _connectionString = connectionString;
        }

        public AzureProviderBuilder Add<TAzureStorage>(Action<ContainerOptions> action)
            where TAzureStorage : IBlobStorage
        {
            var containerOptions = new ContainerOptions();
            action.Invoke(containerOptions);

            var storageOptions = new StorageOptions
            { 
                ConnectionString = _connectionString,
                Container = containerOptions.Container
            };

            var implementationType = TypeHelpers.GetImplementationType<TAzureStorage, AzureBlobStorage, StorageOptions>();
            ServiceCollection.AddScoped(typeof(TAzureStorage), x => Activator.CreateInstance(implementationType, storageOptions));

            return this;
        }
    }
}
