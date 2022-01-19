using System;
using Microsoft.Extensions.DependencyInjection;
using ManagedCode.Storage.Core.Builders;
using ManagedCode.Storage.Core.Helpers;
using ManagedCode.Storage.Core;
using ManagedCode.Storage.Azure.Options;

namespace ManagedCode.Storage.Azure.Extensions
{
    public static class ProviderExtensions
    {
        public static ProviderBuilder AddAzureBlobStorage<TAzureStorage>(
            this ProviderBuilder providerBuilder, 
            Action<AzureBlobStorageConnectionOptions> action)
            where TAzureStorage : IBlobStorage
        {
            var connectionOptions = new AzureBlobStorageConnectionOptions();
            action.Invoke(connectionOptions);

            var implementationType = TypeHelpers.GetImplementationType<TAzureStorage, AzureBlobStorage, AzureBlobStorageConnectionOptions>();
            providerBuilder.ServiceCollection.AddScoped(typeof(TAzureStorage), x => Activator.CreateInstance(implementationType, connectionOptions));

            // Because of AzureBlobStorage does not inherits TAzureStorage, DI complains on unability of casting
            // providerBuilder.ServiceCollection.AddScoped(typeof(TAzureStorage), x => new AzureBlobStorage(connectionOptions));

            return providerBuilder;
        }
    }
}
