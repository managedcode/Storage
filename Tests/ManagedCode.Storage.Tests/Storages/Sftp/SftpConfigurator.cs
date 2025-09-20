using ManagedCode.Storage.Sftp.Extensions;
using ManagedCode.Storage.Sftp.Options;
using Microsoft.Extensions.DependencyInjection;

namespace ManagedCode.Storage.Tests.Storages.Sftp;

/// <summary>
/// Configures DI for SFTP storage tests.
/// </summary>
public static class SftpConfigurator
{
    public static ServiceProvider ConfigureServices(string host, int port, string username, string password, string remoteDirectory)
    {
        var services = new ServiceCollection();

        services.AddLogging();

        services.AddSftpStorageAsDefault(opt =>
        {
            opt.Host = host;
            opt.Port = port;
            opt.Username = username;
            opt.Password = password;
            opt.RemoteDirectory = remoteDirectory;
            opt.CreateContainerIfNotExists = true;
            opt.ConnectTimeout = 30000;
            opt.OperationTimeout = 30000;
        });

        services.AddSftpStorage(new SftpStorageOptions
        {
            Host = host,
            Port = port,
            Username = username,
            Password = password,
            RemoteDirectory = remoteDirectory,
            CreateContainerIfNotExists = true,
            ConnectTimeout = 30000,
            OperationTimeout = 30000
        });

        return services.BuildServiceProvider();
    }
}
