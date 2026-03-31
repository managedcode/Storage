using System;
using ManagedCode.Storage.Browser.Options;
using ManagedCode.Storage.Core;
using ManagedCode.Storage.Core.Exceptions;
using ManagedCode.Storage.Core.Providers;

namespace ManagedCode.Storage.Browser;

public sealed class BrowserStorageProvider(BrowserStorageOptions defaultOptions) : IStorageProvider
{
    public Type StorageOptionsType => typeof(BrowserStorageOptions);

    public TStorage CreateStorage<TStorage, TOptions>(TOptions options)
        where TStorage : class, IStorage
        where TOptions : class, IStorageOptions
    {
        ArgumentNullException.ThrowIfNull(options);

        if (options is not BrowserStorageOptions browserOptions)
            throw new ArgumentException($"Options must be of type {typeof(BrowserStorageOptions)}", nameof(options));

        if (browserOptions.JsRuntime is null)
            throw new BadConfigurationException("Browser storage requires BrowserStorageOptions.JsRuntime to be provided.");

        var storage = new BrowserStorage(browserOptions);
        return storage as TStorage
               ?? throw new InvalidOperationException($"Cannot create storage of type {typeof(TStorage)}");
    }

    public IStorageOptions GetDefaultOptions()
    {
        return new BrowserStorageOptions
        {
            ContainerName = defaultOptions.ContainerName,
            DatabaseName = defaultOptions.DatabaseName,
            ChunkSizeBytes = defaultOptions.ChunkSizeBytes,
            ChunkBatchSize = defaultOptions.ChunkBatchSize,
            CreateContainerIfNotExists = defaultOptions.CreateContainerIfNotExists,
            JsRuntime = defaultOptions.JsRuntime
        };
    }
}
