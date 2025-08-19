using System;
using ManagedCode.Storage.Core;
using ManagedCode.Storage.Core.Providers;
using ManagedCode.Storage.Ftp.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ManagedCode.Storage.Ftp;

/// <summary>
/// Provides FTP storage instances for the storage factory.
/// </summary>
public class FtpStorageProvider : IStorageProvider
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IFtpStorageOptions _defaultOptions;

    public FtpStorageProvider(IServiceProvider serviceProvider, IFtpStorageOptions defaultOptions)
    {
        _serviceProvider = serviceProvider;
        _defaultOptions = defaultOptions;
    }

    public Type StorageOptionsType => typeof(IFtpStorageOptions);

    public TStorage CreateStorage<TStorage, TOptions>(TOptions options)
        where TStorage : class, IStorage
        where TOptions : class, IStorageOptions
    {
        if (options is not IFtpStorageOptions ftpOptions)
        {
            throw new ArgumentException($"Options must be of type {typeof(IFtpStorageOptions)}", nameof(options));
        }

        var logger = _serviceProvider.GetService<ILogger<FtpStorage>>();
        var storage = new FtpStorage(ftpOptions, logger);

        return storage as TStorage
               ?? throw new InvalidOperationException($"Cannot create storage of type {typeof(TStorage)}");
    }

    public IStorageOptions GetDefaultOptions()
    {
        return _defaultOptions switch
        {
            FtpStorageOptions ftpOptions => new FtpStorageOptions
            {
                Host = ftpOptions.Host,
                Port = ftpOptions.Port,
                Username = ftpOptions.Username,
                Password = ftpOptions.Password,
                RemoteDirectory = ftpOptions.RemoteDirectory,
                ConnectTimeout = ftpOptions.ConnectTimeout,
                DataConnectionTimeout = ftpOptions.DataConnectionTimeout,
                CreateDirectoryIfNotExists = ftpOptions.CreateDirectoryIfNotExists,
                CreateContainerIfNotExists = ftpOptions.CreateContainerIfNotExists,
                DataConnectionType = ftpOptions.DataConnectionType,
                Encoding = ftpOptions.Encoding,
                EncryptionMode = ftpOptions.EncryptionMode,
                SslProtocols = ftpOptions.SslProtocols,
                ValidateAnyCertificate = ftpOptions.ValidateAnyCertificate
            },
            FtpsStorageOptions ftpsOptions => new FtpsStorageOptions
            {
                Host = ftpsOptions.Host,
                Port = ftpsOptions.Port,
                Username = ftpsOptions.Username,
                Password = ftpsOptions.Password,
                RemoteDirectory = ftpsOptions.RemoteDirectory,
                ConnectTimeout = ftpsOptions.ConnectTimeout,
                DataConnectionTimeout = ftpsOptions.DataConnectionTimeout,
                CreateDirectoryIfNotExists = ftpsOptions.CreateDirectoryIfNotExists,
                CreateContainerIfNotExists = ftpsOptions.CreateContainerIfNotExists,
                DataConnectionType = ftpsOptions.DataConnectionType,
                Encoding = ftpsOptions.Encoding,
                EncryptionMode = ftpsOptions.EncryptionMode,
                SslProtocols = ftpsOptions.SslProtocols,
                ValidateAnyCertificate = ftpsOptions.ValidateAnyCertificate,
                ClientCertificatePath = ftpsOptions.ClientCertificatePath,
                ClientCertificatePassword = ftpsOptions.ClientCertificatePassword,
                DataConnectionEncryption = ftpsOptions.DataConnectionEncryption
            },
            SftpStorageOptions sftpOptions => new SftpStorageOptions
            {
                Host = sftpOptions.Host,
                Port = sftpOptions.Port,
                Username = sftpOptions.Username,
                Password = sftpOptions.Password,
                RemoteDirectory = sftpOptions.RemoteDirectory,
                ConnectTimeout = sftpOptions.ConnectTimeout,
                DataConnectionTimeout = sftpOptions.DataConnectionTimeout,
                CreateDirectoryIfNotExists = sftpOptions.CreateDirectoryIfNotExists,
                CreateContainerIfNotExists = sftpOptions.CreateContainerIfNotExists,
                DataConnectionType = sftpOptions.DataConnectionType,
                Encoding = sftpOptions.Encoding,
                PrivateKeyPath = sftpOptions.PrivateKeyPath,
                PrivateKeyPassphrase = sftpOptions.PrivateKeyPassphrase,
                PrivateKeyContent = sftpOptions.PrivateKeyContent,
                AcceptAnyHostKey = sftpOptions.AcceptAnyHostKey,
                HostKeyFingerprint = sftpOptions.HostKeyFingerprint
            },
            _ => throw new ArgumentException($"Unknown options type: {_defaultOptions.GetType()}")
        };
    }
}