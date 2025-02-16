using System;
using System.Collections.Generic;
using ManagedCode.Storage.Azure;
using ManagedCode.Storage.Azure.Options;
using ManagedCode.Storage.Core;
using ManagedCode.Storage.Core.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Storage.Providers
{
    public class AzureStorageProvider(IServiceProvider serviceProvider, AzureStorageOptions defaultOptions) : IStorageProvider
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
            return defaultOptions.DeepCopy();
        }
    }
}
