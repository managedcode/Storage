using System;
using System.Collections.Generic;
using System.Linq;

namespace ManagedCode.Storage.Core.Providers
{
    public class StorageFactory : IStorageFactory
    {
        public StorageFactory(IEnumerable<IStorageProvider> providers)
        {
            Providers = providers.ToDictionary(p => p.StorageOptionsType, p => p);
        }

        private IStorageProvider? GetProvider(Type optionsType)
        {
            return Providers
                .FirstOrDefault(x => x.Key.IsAssignableFrom(optionsType))
                .Value;
        }

        public Dictionary<Type, IStorageProvider> Providers { get; set; }

        public IStorage CreateStorage(IStorageOptions options)
        {
            var provider = GetProvider(options.GetType())
                           ?? throw new NotSupportedException($"Provider for {options.GetType()} not found");

            return provider.CreateStorage<IStorage, IStorageOptions>(options);
        }

        public IStorage CreateStorage(Action<IStorageOptions> options)
        {
            var provider = GetProvider(options.GetType())
                           ?? throw new NotSupportedException($"Provider for {options.GetType()} not found");

            var storageOptions = provider.GetDefaultOptions();
            options.Invoke(storageOptions);
            return CreateStorage(storageOptions);
        }

        public TStorage CreateStorage<TStorage, TOptions>(TOptions options)
            where TStorage : class, IStorage
            where TOptions : class, IStorageOptions
        {
            var provider = GetProvider(typeof(TOptions))
                           ?? throw new NotSupportedException($"Provider for {typeof(TOptions)} not found");

            return provider.CreateStorage<TStorage, TOptions>(options);
        }

        public TStorage CreateStorage<TStorage, TOptions>(Action<TOptions> options)
            where TStorage : class, IStorage
            where TOptions : class, IStorageOptions
        {
            var provider = GetProvider(typeof(TOptions))
                           ?? throw new NotSupportedException($"Provider for {typeof(TOptions)} not found");

            TOptions storageOptions = (TOptions)provider.GetDefaultOptions();
            options.Invoke(storageOptions);
            return provider.CreateStorage<TStorage, TOptions>(storageOptions);
        }
    }
}