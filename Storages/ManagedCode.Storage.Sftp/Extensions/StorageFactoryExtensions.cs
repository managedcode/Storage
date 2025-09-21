using System;
using ManagedCode.Storage.Core.Providers;
using ManagedCode.Storage.Sftp.Options;

namespace ManagedCode.Storage.Sftp.Extensions;

/// <summary>
/// Factory helpers for creating SFTP storage instances.
/// </summary>
public static class StorageFactoryExtensions
{
    public static ISftpStorage CreateSftpStorage(this IStorageFactory factory, Action<SftpStorageOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(factory);
        ArgumentNullException.ThrowIfNull(configure);

        return factory.CreateStorage<ISftpStorage, SftpStorageOptions>(configure);
    }

    public static ISftpStorage CreateSftpStorage(this IStorageFactory factory, SftpStorageOptions options)
    {
        ArgumentNullException.ThrowIfNull(factory);
        ArgumentNullException.ThrowIfNull(options);

        return factory.CreateStorage<ISftpStorage, SftpStorageOptions>(options);
    }

    public static ISftpStorage CreateSftpStorageWithPassword(this IStorageFactory factory,
        string host,
        string username,
        string password,
        int port = 22,
        string? remoteDirectory = "/")
    {
        ArgumentNullException.ThrowIfNull(factory);

        var options = new SftpStorageOptions
        {
            Host = host,
            Port = port,
            Username = username,
            Password = password,
            RemoteDirectory = remoteDirectory
        };

        return factory.CreateStorage<ISftpStorage, SftpStorageOptions>(options);
    }

    public static ISftpStorage CreateSftpStorageWithPrivateKey(this IStorageFactory factory,
        string host,
        string username,
        string privateKeyPath,
        string? privateKeyPassphrase = null,
        int port = 22,
        string? remoteDirectory = "/")
    {
        ArgumentNullException.ThrowIfNull(factory);

        var options = new SftpStorageOptions
        {
            Host = host,
            Port = port,
            Username = username,
            RemoteDirectory = remoteDirectory,
            PrivateKeyPath = privateKeyPath,
            PrivateKeyPassphrase = privateKeyPassphrase
        };

        return factory.CreateStorage<ISftpStorage, SftpStorageOptions>(options);
    }

    public static ISftpStorage CreateSftpStorageWithPrivateKeyContent(this IStorageFactory factory,
        string host,
        string username,
        string privateKeyContent,
        string? privateKeyPassphrase = null,
        int port = 22,
        string? remoteDirectory = "/")
    {
        ArgumentNullException.ThrowIfNull(factory);

        var options = new SftpStorageOptions
        {
            Host = host,
            Port = port,
            Username = username,
            RemoteDirectory = remoteDirectory,
            PrivateKeyContent = privateKeyContent,
            PrivateKeyPassphrase = privateKeyPassphrase
        };

        return factory.CreateStorage<ISftpStorage, SftpStorageOptions>(options);
    }
}
