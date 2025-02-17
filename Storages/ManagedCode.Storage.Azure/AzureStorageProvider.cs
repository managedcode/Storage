using System;
using ManagedCode.Storage.Azure.Options;
using ManagedCode.Storage.Core;
using ManagedCode.Storage.Core.Extensions;
using ManagedCode.Storage.Core.Providers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ManagedCode.Storage.Azure
{
    public class AzureStorageProvider(IServiceProvider serviceProvider, IAzureStorageOptions defaultOptions) : IStorageProvider
    {
        public Type StorageOptionsType => typeof(IAzureStorageOptions);
        
        public TStorage CreateStorage<TStorage, TOptions>(TOptions options) 
            where TStorage : class, IStorage 
            where TOptions : class, IStorageOptions
        {
            if (options is not IAzureStorageOptions azureOptions)
            {
                throw new ArgumentException($"Options must be of type {typeof(IAzureStorageOptions)}", nameof(options));
            }

            var logger = serviceProvider.GetService<ILogger<AzureStorage>>();
            var storage = new AzureStorage(azureOptions, logger);

            return storage as TStorage 
                   ?? throw new InvalidOperationException($"Cannot create storage of type {typeof(TStorage)}");
        }

        public IStorageOptions GetDefaultOptions()
        {
            return defaultOptions switch
            {
                AzureStorageCredentialsOptions credentialsOptions => new AzureStorageCredentialsOptions
                {
                    AccountName = credentialsOptions.AccountName,
                    ContainerName = credentialsOptions.ContainerName,
                    Credentials = credentialsOptions.Credentials,
                    Container = credentialsOptions.Container,
                    PublicAccessType = credentialsOptions.PublicAccessType,
                    OriginalOptions = credentialsOptions.OriginalOptions,
                    CreateContainerIfNotExists = credentialsOptions.CreateContainerIfNotExists
                },
                AzureStorageOptions storageOptions => new AzureStorageOptions
                {
                    ConnectionString = storageOptions.ConnectionString,
                    Container = storageOptions.Container,
                    PublicAccessType = storageOptions.PublicAccessType,
                    OriginalOptions = storageOptions.OriginalOptions,
                    CreateContainerIfNotExists = storageOptions.CreateContainerIfNotExists
                },
                _ => throw new ArgumentException($"Unknown options type: {defaultOptions.GetType()}")
            };
        }
    }
}
