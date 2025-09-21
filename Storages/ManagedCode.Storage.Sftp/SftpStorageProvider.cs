using System;
using ManagedCode.Storage.Core;
using ManagedCode.Storage.Core.Providers;
using ManagedCode.Storage.Sftp.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ManagedCode.Storage.Sftp;

/// <summary>
/// Factory wrapper that allows the storage factory to build SFTP storage instances on demand.
/// </summary>
public class SftpStorageProvider : IStorageProvider
{
    private readonly IServiceProvider _serviceProvider;

    public SftpStorageProvider(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public Type StorageOptionsType => typeof(SftpStorageOptions);

    public TStorage CreateStorage<TStorage, TOptions>(TOptions options)
        where TStorage : class, IStorage
        where TOptions : class, IStorageOptions
    {
        if (options is not SftpStorageOptions sftpOptions)
        {
            throw new ArgumentException($"Options must be of type {typeof(SftpStorageOptions)}", nameof(options));
        }

        var logger = _serviceProvider.GetRequiredService<ILogger<SftpStorage>>();
        var storage = new SftpStorage(sftpOptions, logger);
        if (storage is TStorage typed)
        {
            return typed;
        }

        throw new InvalidOperationException($"Cannot create storage of type {typeof(TStorage)} using {typeof(SftpStorage)}");
    }

    public IStorageOptions GetDefaultOptions()
    {
        var defaults = _serviceProvider.GetRequiredService<SftpStorageOptions>();
        return new SftpStorageOptions
        {
            Host = defaults.Host,
            Port = defaults.Port,
            Username = defaults.Username,
            Password = defaults.Password,
            RemoteDirectory = defaults.RemoteDirectory,
            ConnectTimeout = defaults.ConnectTimeout,
            OperationTimeout = defaults.OperationTimeout,
            CreateDirectoryIfNotExists = defaults.CreateDirectoryIfNotExists,
            CreateContainerIfNotExists = defaults.CreateContainerIfNotExists,
            PrivateKeyPath = defaults.PrivateKeyPath,
            PrivateKeyPassphrase = defaults.PrivateKeyPassphrase,
            PrivateKeyContent = defaults.PrivateKeyContent,
            AcceptAnyHostKey = defaults.AcceptAnyHostKey,
            HostKeyFingerprint = defaults.HostKeyFingerprint
        };
    }
}
