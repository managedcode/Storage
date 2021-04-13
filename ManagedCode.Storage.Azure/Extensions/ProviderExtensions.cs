using System;
using ManagedCode.Storage.Core.Builders;
using ManagedCode.Storage.Azure.Options;
using ManagedCode.Storage.Azure.Builders;

namespace ManagedCode.Storage.Azure.Extensions
{
    public static class ProviderExtensions
    {
        public static AzureProviderBuilder AddAzureBlobStorage(
            this ProviderBuilder providerBuilder, 
            Action<ConnectionOptions> action)
        {
            var connectionOptions = new ConnectionOptions();
            action.Invoke(connectionOptions);

            return new AzureProviderBuilder(providerBuilder.ServiceCollection, connectionOptions.ConnectionString);
        }
    }
}
