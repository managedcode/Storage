using System;
using ManagedCode.Storage.Core;
using ManagedCode.Storage.Core.Extensions;
using ManagedCode.Storage.Core.Providers;
using ManagedCode.Storage.Google.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ManagedCode.Storage.Google
{
    public class GCPStorageProvider(IServiceProvider serviceProvider, GCPStorageOptions defaultOptions) : IStorageProvider
    {
        public Type StorageOptionsType => typeof(GCPStorageOptions);
        
        public TStorage CreateStorage<TStorage, TOptions>(TOptions options) 
            where TStorage : class, IStorage 
            where TOptions : class, IStorageOptions
        {
            if (options is not GCPStorageOptions azureOptions)
            {
                throw new ArgumentException($"Options must be of type {typeof(GCPStorageOptions)}", nameof(options));
            }

            var logger = serviceProvider.GetService<ILogger<GCPStorage>>();
            var storage = new GCPStorage(azureOptions, logger);

            return storage as TStorage 
                   ?? throw new InvalidOperationException($"Cannot create storage of type {typeof(TStorage)}");
        }

        public IStorageOptions GetDefaultOptions()
        {
            return defaultOptions.DeepCopy();
        }
    }
}
