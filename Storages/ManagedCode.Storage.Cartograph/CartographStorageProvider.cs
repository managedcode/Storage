using System;
using ManagedCode.Storage.Cartograph.Options;
using ManagedCode.Storage.Core;
using ManagedCode.Storage.Core.Providers;

namespace ManagedCode.Storage.Cartograph;

public sealed class CartographStorageProvider(CartographStorageOptions defaultOptions) : IStorageProvider
{
    public Type StorageOptionsType => typeof(CartographStorageOptions);

    public TStorage CreateStorage<TStorage, TOptions>(TOptions options)
        where TStorage : class, IStorage
        where TOptions : class, IStorageOptions
    {
        if (options is not CartographStorageOptions artifactOptions)
            throw new ArgumentException($"Options must be of type {nameof(CartographStorageOptions)}.", nameof(options));
        if (!typeof(TStorage).IsAssignableFrom(typeof(CartographStorage)))
            throw new InvalidOperationException($"Cannot create storage of type {typeof(TStorage)}.");

        return (TStorage)(IStorage)new CartographStorage(artifactOptions);
    }

    public IStorageOptions GetDefaultOptions() => new CartographStorageOptions
    {
        ArtifactPath = defaultOptions.ArtifactPath,
        CreateContainerIfNotExists = defaultOptions.CreateContainerIfNotExists
    };
}
