using System;
using ManagedCode.Storage.Aws.Options;
using ManagedCode.Storage.Core;
using ManagedCode.Storage.Core.Extensions;
using ManagedCode.Storage.Core.Providers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ManagedCode.Storage.Aws
{
    public class AWSStorageProvider(IServiceProvider serviceProvider, AWSStorageOptions defaultOptions) : IStorageProvider
    {
        public Type StorageOptionsType => typeof(AWSStorageOptions);
        
        public TStorage CreateStorage<TStorage, TOptions>(TOptions options) 
            where TStorage : class, IStorage 
            where TOptions : class, IStorageOptions
        {
            if (options is not AWSStorageOptions azureOptions)
            {
                throw new ArgumentException($"Options must be of type {typeof(AWSStorageOptions)}", nameof(options));
            }

            var logger = serviceProvider.GetService<ILogger<AWSStorage>>();
            var storage = new AWSStorage(azureOptions, logger);

            return storage as TStorage 
                   ?? throw new InvalidOperationException($"Cannot create storage of type {typeof(TStorage)}");
        }

        public IStorageOptions GetDefaultOptions()
        {
            return defaultOptions.DeepCopy();
        }
    }
}
