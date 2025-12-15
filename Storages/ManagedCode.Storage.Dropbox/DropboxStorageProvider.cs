using System;
using ManagedCode.Storage.Core;
using ManagedCode.Storage.Core.Extensions;
using ManagedCode.Storage.Core.Providers;
using ManagedCode.Storage.Dropbox.Options;
using Microsoft.Extensions.Logging;

namespace ManagedCode.Storage.Dropbox;

public class DropboxStorageProvider(IServiceProvider serviceProvider, DropboxStorageOptions defaultOptions) : IStorageProvider
{
    public Type StorageOptionsType => typeof(DropboxStorageOptions);

    public TStorage CreateStorage<TStorage, TOptions>(TOptions options)
        where TStorage : class, IStorage
        where TOptions : class, IStorageOptions
    {
        if (options is not DropboxStorageOptions dropboxOptions)
        {
            throw new ArgumentException($"Options must be of type {typeof(DropboxStorageOptions)}", nameof(options));
        }

        var logger = serviceProvider.GetService(typeof(ILogger<DropboxStorage>)) as ILogger<DropboxStorage>;
        var storage = new DropboxStorage(dropboxOptions, logger);
        return storage as TStorage ?? throw new InvalidOperationException($"Cannot create storage of type {typeof(TStorage)}");
    }

    public IStorageOptions GetDefaultOptions()
    {
        return new DropboxStorageOptions
        {
            RootPath = defaultOptions.RootPath,
            DropboxClient = defaultOptions.DropboxClient,
            Client = defaultOptions.Client,
            AccessToken = defaultOptions.AccessToken,
            RefreshToken = defaultOptions.RefreshToken,
            AppKey = defaultOptions.AppKey,
            AppSecret = defaultOptions.AppSecret,
            DropboxClientConfig = defaultOptions.DropboxClientConfig,
            CreateContainerIfNotExists = defaultOptions.CreateContainerIfNotExists
        };
    }
}
