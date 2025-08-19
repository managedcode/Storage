using System;
using ManagedCode.Storage.Core.Providers;
using ManagedCode.Storage.Ftp.Options;

namespace ManagedCode.Storage.Ftp.Extensions;

/// <summary>
/// Extension methods for StorageFactory to create FTP storage instances.
/// </summary>
public static class StorageFactoryExtensions
{
    #region FTP Extensions

    public static IFtpStorage CreateFtpStorage(this IStorageFactory factory, Action<FtpStorageOptions> options)
    {
        return factory.CreateStorage<IFtpStorage, FtpStorageOptions>(options);
    }

    public static IFtpStorage CreateFtpStorage(this IStorageFactory factory, FtpStorageOptions options)
    {
        return factory.CreateStorage<IFtpStorage, FtpStorageOptions>(options);
    }

    public static IFtpStorage CreateFtpStorage(this IStorageFactory factory, 
        string host, 
        int port = 21, 
        string? username = null, 
        string? password = null,
        string? remoteDirectory = "/")
    {
        var options = new FtpStorageOptions
        {
            Host = host,
            Port = port,
            Username = username,
            Password = password,
            RemoteDirectory = remoteDirectory
        };

        return factory.CreateStorage<IFtpStorage, FtpStorageOptions>(options);
    }

    #endregion

    #region FTPS Extensions

    public static IFtpStorage CreateFtpsStorage(this IStorageFactory factory, Action<FtpsStorageOptions> options)
    {
        return factory.CreateStorage<IFtpStorage, FtpsStorageOptions>(options);
    }

    public static IFtpStorage CreateFtpsStorage(this IStorageFactory factory, FtpsStorageOptions options)
    {
        return factory.CreateStorage<IFtpStorage, FtpsStorageOptions>(options);
    }

    public static IFtpStorage CreateFtpsStorage(this IStorageFactory factory,
        string host,
        int port = 990,
        string? username = null,
        string? password = null,
        string? remoteDirectory = "/",
        FluentFTP.FtpEncryptionMode encryptionMode = FluentFTP.FtpEncryptionMode.Implicit)
    {
        var options = new FtpsStorageOptions
        {
            Host = host,
            Port = port,
            Username = username,
            Password = password,
            RemoteDirectory = remoteDirectory,
            EncryptionMode = encryptionMode
        };

        return factory.CreateStorage<IFtpStorage, FtpsStorageOptions>(options);
    }

    #endregion

    #region SFTP Extensions

    public static IFtpStorage CreateSftpStorage(this IStorageFactory factory, Action<SftpStorageOptions> options)
    {
        return factory.CreateStorage<IFtpStorage, SftpStorageOptions>(options);
    }

    public static IFtpStorage CreateSftpStorage(this IStorageFactory factory, SftpStorageOptions options)
    {
        return factory.CreateStorage<IFtpStorage, SftpStorageOptions>(options);
    }

    public static IFtpStorage CreateSftpStorageWithPassword(this IStorageFactory factory,
        string host,
        int port = 22,
        string? username = null,
        string? password = null,
        string? remoteDirectory = "/")
    {
        var options = new SftpStorageOptions
        {
            Host = host,
            Port = port,
            Username = username,
            Password = password,
            RemoteDirectory = remoteDirectory
        };

        return factory.CreateStorage<IFtpStorage, SftpStorageOptions>(options);
    }

    public static IFtpStorage CreateSftpStorageWithPrivateKey(this IStorageFactory factory,
        string host,
        string username,
        string privateKeyPath,
        string? privateKeyPassphrase = null,
        int port = 22,
        string? remoteDirectory = "/")
    {
        var options = new SftpStorageOptions
        {
            Host = host,
            Port = port,
            Username = username,
            RemoteDirectory = remoteDirectory,
            PrivateKeyPath = privateKeyPath,
            PrivateKeyPassphrase = privateKeyPassphrase
        };

        return factory.CreateStorage<IFtpStorage, SftpStorageOptions>(options);
    }

    public static IFtpStorage CreateSftpStorageWithPrivateKeyContent(this IStorageFactory factory,
        string host,
        string username,
        string privateKeyContent,
        string? privateKeyPassphrase = null,
        int port = 22,
        string? remoteDirectory = "/")
    {
        var options = new SftpStorageOptions
        {
            Host = host,
            Port = port,
            Username = username,
            RemoteDirectory = remoteDirectory,
            PrivateKeyContent = privateKeyContent,
            PrivateKeyPassphrase = privateKeyPassphrase
        };

        return factory.CreateStorage<IFtpStorage, SftpStorageOptions>(options);
    }

    #endregion
}