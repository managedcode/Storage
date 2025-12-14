using System;
using ManagedCode.Storage.CloudKit.Options;
using ManagedCode.Storage.Core;
using ManagedCode.Storage.Core.Providers;
using Microsoft.Extensions.Logging;

namespace ManagedCode.Storage.CloudKit;

public class CloudKitStorageProvider(IServiceProvider serviceProvider, CloudKitStorageOptions defaultOptions) : IStorageProvider
{
    public Type StorageOptionsType => typeof(CloudKitStorageOptions);

    public TStorage CreateStorage<TStorage, TOptions>(TOptions options)
        where TStorage : class, IStorage
        where TOptions : class, IStorageOptions
    {
        if (options is not CloudKitStorageOptions cloudKitOptions)
        {
            throw new ArgumentException($"Options must be of type {typeof(CloudKitStorageOptions)}", nameof(options));
        }

        var logger = serviceProvider.GetService(typeof(ILogger<CloudKitStorage>)) as ILogger<CloudKitStorage>;
        var storage = new CloudKitStorage(cloudKitOptions, logger);
        return storage as TStorage ?? throw new InvalidOperationException($"Cannot create storage of type {typeof(TStorage)}");
    }

    public IStorageOptions GetDefaultOptions()
    {
        return new CloudKitStorageOptions
        {
            ContainerId = defaultOptions.ContainerId,
            Environment = defaultOptions.Environment,
            Database = defaultOptions.Database,
            RootPath = defaultOptions.RootPath,
            RecordType = defaultOptions.RecordType,
            PathFieldName = defaultOptions.PathFieldName,
            AssetFieldName = defaultOptions.AssetFieldName,
            ContentTypeFieldName = defaultOptions.ContentTypeFieldName,
            ApiToken = defaultOptions.ApiToken,
            WebAuthToken = defaultOptions.WebAuthToken,
            ServerToServerKeyId = defaultOptions.ServerToServerKeyId,
            ServerToServerPrivateKeyPem = defaultOptions.ServerToServerPrivateKeyPem,
            HttpClient = defaultOptions.HttpClient,
            Client = defaultOptions.Client,
            CreateContainerIfNotExists = defaultOptions.CreateContainerIfNotExists
        };
    }
}

