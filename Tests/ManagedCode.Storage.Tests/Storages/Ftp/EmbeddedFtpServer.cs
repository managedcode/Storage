using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using FubarDev.FtpServer;
using FubarDev.FtpServer.FileSystem.InMemory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ManagedCode.Storage.Tests.Storages.Ftp;

/// <summary>
/// Embedded FTP server for testing using FubarDev.FtpServer with in-memory filesystem.
/// </summary>
public class EmbeddedFtpServer : IAsyncDisposable
{
    private readonly IFtpServerHost _ftpServerHost;
    private readonly ServiceProvider _serviceProvider;
    
    public const string Username = "anonymous";
    public const string Password = "anonymous@example.com";
    public int Port { get; }
    public string Host { get; } = "127.0.0.1";
    
    public EmbeddedFtpServer(int port = 0) // 0 = use any available port
    {
        Port = port == 0 ? GetAvailablePort() : port;
        
        // Build service collection with FTP server and in-memory file system
        var services = new ServiceCollection();
        
        // Add logging
        services.AddLogging(builder => builder.AddConsole());
        
        // Add FTP server with in-memory file system  
        services.AddFtpServer(builder => builder
            .UseInMemoryFileSystem() // Use in-memory file system for testing
            .EnableAnonymousAuthentication() // Allow anonymous access for simplicity
        );
        
        // Configure FTP server
        services.Configure<FtpServerOptions>(opt =>
        {
            opt.ServerAddress = Host;
            opt.Port = Port;
            opt.MaxActiveConnections = 10;
            opt.ConnectionInactivityCheckInterval = TimeSpan.FromSeconds(10);
        });
        
        _serviceProvider = services.BuildServiceProvider();
        _ftpServerHost = _serviceProvider.GetRequiredService<IFtpServerHost>();
    }
    
    /// <summary>
    /// Start the FTP server.
    /// </summary>
    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        await _ftpServerHost.StartAsync(cancellationToken);
    }
    
    /// <summary>
    /// Stop the FTP server.
    /// </summary>
    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        await _ftpServerHost.StopAsync(cancellationToken);
    }
    
    /// <summary>
    /// Get connection string for this FTP server.
    /// </summary>
    public string GetConnectionString()
    {
        return $"ftp://{Host}:{Port}";
    }
    
    /// <summary>
    /// Get connection string with credentials (for anonymous access).
    /// </summary>
    public string GetConnectionStringWithCredentials()
    {
        return $"ftp://anonymous:anonymous@example.com@{Host}:{Port}";
    }
    
    public async ValueTask DisposeAsync()
    {
        if (_ftpServerHost != null)
        {
            await _ftpServerHost.StopAsync(CancellationToken.None);
        }
        
        _serviceProvider?.Dispose();
    }
    
    /// <summary>
    /// Get an available port for the FTP server.
    /// </summary>
    private static int GetAvailablePort()
    {
        using var listener = new System.Net.Sockets.TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return port;
    }
}