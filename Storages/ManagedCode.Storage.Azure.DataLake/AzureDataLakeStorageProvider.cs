using System;
using ManagedCode.Storage.Azure.DataLake.Options;
using ManagedCode.Storage.Core;
using ManagedCode.Storage.Core.Extensions;
using ManagedCode.Storage.Core.Providers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ManagedCode.Storage.Azure.DataLake
{
    public class AzureDataLakeStorageProvider(IServiceProvider serviceProvider, AzureDataLakeStorageOptions defaultOptions) : IStorageProvider
    {
        public Type StorageOptionsType => typeof(AzureDataLakeStorageOptions);
        
        public TStorage CreateStorage<TStorage, TOptions>(TOptions options) 
            where TStorage : class, IStorage 
            where TOptions : class, IStorageOptions
        {
            if (options is not AzureDataLakeStorageOptions azureOptions)
            {
                throw new ArgumentException($"Options must be of type {typeof(AzureDataLakeStorageOptions)}", nameof(options));
            }

            var logger = serviceProvider.GetService<ILogger<AzureDataLakeStorage>>();
            var storage = new AzureDataLakeStorage(azureOptions, logger);

            return storage as TStorage 
                   ?? throw new InvalidOperationException($"Cannot create storage of type {typeof(TStorage)}");
        }

        public IStorageOptions GetDefaultOptions()
        {
            return new AzureDataLakeStorageOptions()
            {
                ConnectionString = defaultOptions.ConnectionString,
                FileSystem = defaultOptions.FileSystem,
                PublicAccessType = defaultOptions.PublicAccessType,
                CreateContainerIfNotExists = defaultOptions.CreateContainerIfNotExists
            };
        }
    }
}