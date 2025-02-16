using System;
using System.Collections.Generic;
using System.Linq;

namespace ManagedCode.Storage.Core.Providers
{
    public class StorageFactory : IStorageFactory
    {
        public StorageFactory(IEnumerable<IStorageProvider> providers)
        {
            Providers = providers.ToDictionary(p => p.StorageOptionsType);
        }

        public Dictionary<Type, IStorageProvider> Providers { get; set; }

        public IStorage CreateStorage(IStorageOptions options)
        {
            if (Providers.TryGetValue(options.GetType(), out var provider))
            {
                return provider.CreateStorage<IStorage, IStorageOptions>(options);
            }

            throw new NotSupportedException($"Provider for {options.GetType()} not found");
        }

        public IStorage CreateStorage(Action<IStorageOptions> options)
        {
            if (Providers.TryGetValue(options.GetType(), out var provider))
            {
                var storageOptions = provider.GetDefaultOptions();
                options.Invoke(storageOptions);
                return CreateStorage(storageOptions);
            }

            throw new NotSupportedException($"Provider for {options.GetType()} not found");
        }

        public TStorage CreateStorage<TStorage, TOptions>(TOptions options) 
            where TStorage : class, IStorage 
            where TOptions : class, IStorageOptions
        {
            if (Providers.TryGetValue(typeof(TOptions), out var provider))
            {
                return provider.CreateStorage<TStorage, TOptions>(options);
            }

            throw new NotSupportedException($"Provider for {typeof(TOptions)} not found");
        }

        public TStorage CreateStorage<TStorage, TOptions>(Action<TOptions> options) 
            where TStorage : class, IStorage 
            where TOptions : class, IStorageOptions
        {
            if (Providers.TryGetValue(typeof(TOptions), out var provider))
            {
                TOptions storageOptions = (TOptions)provider.GetDefaultOptions();
                options.Invoke(storageOptions);
                return provider.CreateStorage<TStorage, TOptions>(storageOptions);
            }

            throw new NotSupportedException($"Provider for {typeof(TOptions)} not found");
           
        }
    }
}