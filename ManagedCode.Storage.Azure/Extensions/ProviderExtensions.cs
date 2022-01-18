using System;
using Microsoft.Extensions.DependencyInjection;
using Azure.Storage.Blobs;
using ManagedCode.Storage.Core.Builders;

namespace ManagedCode.Storage.Azure.Extensions
{
    public static class ProviderExtensions
    {
        public static ProviderBuilder AddAzureBlobStorage(this ProviderBuilder providerBuilder, Action<AzureBlobStorageConnectionOptions> action)
        {
            var connectionOptions = new AzureBlobStorageConnectionOptions();
            action.Invoke(connectionOptions);

            var blobServiceClient = new BlobServiceClient(connectionOptions.ConnectionString);
            providerBuilder.ServiceCollection.AddSingleton(blobServiceClient);

            return providerBuilder;
        }
    }
}
