using System;
using ManagedCode.Storage.Ftp.Extensions;
using ManagedCode.Storage.Ftp.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ManagedCode.Storage.Tests.Storages.Ftp;

/// <summary>
/// Configurator for FTP storage tests.
/// </summary>
public class FtpConfigurator
{
    public static ServiceProvider ConfigureServices(string host, int port, string username, string password, string remoteDirectory = "/")
    {
        var services = new ServiceCollection();

        // Add logging services required by FtpStorage
        services.AddLogging();

        services.AddFtpStorageAsDefault(opt =>
        {
            opt.Host = host;
            opt.Port = port;
            opt.Username = username;
            opt.Password = password;
            opt.RemoteDirectory = remoteDirectory;
            opt.CreateContainerIfNotExists = true;
            opt.ConnectTimeout = 30000;
            opt.DataConnectionTimeout = 30000;
            opt.DataConnectionType = FluentFTP.FtpDataConnectionType.AutoActive; // Use active mode for embedded server
        });

        services.AddFtpStorage(new FtpStorageOptions
        {
            Host = host,
            Port = port,
            Username = username,
            Password = password,
            RemoteDirectory = remoteDirectory,
            CreateContainerIfNotExists = true,
            ConnectTimeout = 30000,
            DataConnectionTimeout = 30000
        });

        return services.BuildServiceProvider();
    }

    public static ServiceProvider ConfigureFtpsServices(string host, int port, string username, string password, string remoteDirectory = "/")
    {
        var services = new ServiceCollection();

        // Add logging services required by FtpStorage
        services.AddLogging();

        services.AddFtpsStorageAsDefault(opt =>
        {
            opt.Host = host;
            opt.Port = port;
            opt.Username = username;
            opt.Password = password;
            opt.RemoteDirectory = remoteDirectory;
            opt.CreateContainerIfNotExists = true;
            opt.ConnectTimeout = 30000;
            opt.DataConnectionTimeout = 30000;
            opt.DataConnectionType = FluentFTP.FtpDataConnectionType.AutoActive; // Use active mode for embedded server
            opt.EncryptionMode = FluentFTP.FtpEncryptionMode.Explicit;
            opt.ValidateAnyCertificate = true;
        });

        services.AddFtpsStorage(new FtpsStorageOptions
        {
            Host = host,
            Port = port,
            Username = username,
            Password = password,
            RemoteDirectory = remoteDirectory,
            CreateContainerIfNotExists = true,
            ConnectTimeout = 30000,
            DataConnectionTimeout = 30000,
            EncryptionMode = FluentFTP.FtpEncryptionMode.Explicit,
            ValidateAnyCertificate = true
        });

        return services.BuildServiceProvider();
    }
}