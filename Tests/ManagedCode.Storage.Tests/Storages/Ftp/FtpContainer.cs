using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using DotNet.Testcontainers.Configurations;
using DotNet.Testcontainers.Containers;
using DotNet.Testcontainers.Images;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace ManagedCode.Storage.Tests.Storages.Ftp;

/// <summary>
/// FTP container using embedded FTP server for testing.
/// </summary>
public sealed class FtpContainer : IContainer
{
    private readonly EmbeddedFtpServer _ftpServer;
    
    public const string Username = EmbeddedFtpServer.Username;
    public const string Password = EmbeddedFtpServer.Password;
    
    public FtpContainer()
    {
        _ftpServer = new EmbeddedFtpServer();
    }
    
    // FTP-specific helper methods
    public string GetHost() => _ftpServer.Host;
    public int GetPort() => _ftpServer.Port;
    
    public string GetConnectionString()
    {
        return _ftpServer.GetConnectionStringWithCredentials();
    }
    
    // IContainer implementation
    public async ValueTask DisposeAsync()
    {
        await _ftpServer.DisposeAsync();
    }

    public ushort GetMappedPublicPort(int containerPort)
    {
        return (ushort)_ftpServer.Port;
    }

    public ushort GetMappedPublicPort(string containerPort)
    {
        return (ushort)_ftpServer.Port;
    }

    public Task<long> GetExitCodeAsync(CancellationToken ct = default)
    {
        return Task.FromResult(0L);
    }

    public Task<(string Stdout, string Stderr)> GetLogsAsync(DateTime since = default, DateTime until = default, bool timestampsEnabled = true, CancellationToken ct = default)
    {
        return Task.FromResult(("", ""));
    }

    public async Task StartAsync(CancellationToken ct = default)
    {
        await _ftpServer.StartAsync(ct);
    }

    public async Task StopAsync(CancellationToken ct = default)
    {
        await _ftpServer.StopAsync(ct);
    }

    public Task PauseAsync(CancellationToken ct = default)
    {
        return Task.CompletedTask;
    }

    public Task UnpauseAsync(CancellationToken ct = default)
    {
        return Task.CompletedTask;
    }

    public Task CopyAsync(byte[] fileContent, string filePath, UnixFileModes fileMode = UnixFileModes.None | UnixFileModes.OtherRead | UnixFileModes.GroupRead | UnixFileModes.UserWrite | UnixFileModes.UserRead, CancellationToken ct = default)
    {
        return Task.CompletedTask;
    }

    public Task CopyAsync(string source, string target, UnixFileModes fileMode = UnixFileModes.None | UnixFileModes.OtherRead | UnixFileModes.GroupRead | UnixFileModes.UserWrite | UnixFileModes.UserRead, CancellationToken ct = default)
    {
        return Task.CompletedTask;
    }

    public Task CopyAsync(DirectoryInfo source, string target, UnixFileModes fileMode = UnixFileModes.None | UnixFileModes.OtherRead | UnixFileModes.GroupRead | UnixFileModes.UserWrite | UnixFileModes.UserRead, CancellationToken ct = default)
    {
        return Task.CompletedTask;
    }

    public Task CopyAsync(FileInfo source, string target, UnixFileModes fileMode = UnixFileModes.None | UnixFileModes.OtherRead | UnixFileModes.GroupRead | UnixFileModes.UserWrite | UnixFileModes.UserRead, CancellationToken ct = default)
    {
        return Task.CompletedTask;
    }

    public Task<byte[]> ReadFileAsync(string filePath, CancellationToken ct = default)
    {
        return Task.FromResult(Array.Empty<byte>());
    }

    public Task<ExecResult> ExecAsync(IList<string> command, CancellationToken ct = default)
    {
        return Task.FromResult(new ExecResult("0", "", 0));
    }

    // Properties
    public DateTime CreatedTime { get; } = DateTime.UtcNow;
    public DateTime StartedTime { get; } = DateTime.UtcNow;
    public DateTime StoppedTime { get; } = DateTime.MinValue;
    public DateTime PausedTime { get; } = DateTime.MinValue;
    public DateTime UnpausedTime { get; } = DateTime.MinValue;

    public ILogger Logger { get; } = NullLogger.Instance;
    public string Id { get; } = "embedded-ftp";
    public string Name { get; } = "embedded-ftp";
    public string IpAddress => _ftpServer.Host;
    public string MacAddress { get; } = "00:00:00:00:00:00";
    public string Hostname => _ftpServer.Host;
    public IImage Image { get; } = new DockerImage("embedded-ftp");
    public TestcontainersStates State { get; } = TestcontainersStates.Running;
    public TestcontainersHealthStatus Health { get; } = TestcontainersHealthStatus.Healthy;
    public long HealthCheckFailingStreak { get; } = 0;
    
    // Events (not used but required by interface)
    public event EventHandler? Creating;
    public event EventHandler? Starting;
    public event EventHandler? Stopping;
    public event EventHandler? Pausing;
    public event EventHandler? Unpausing;
    public event EventHandler? Created;
    public event EventHandler? Started;
    public event EventHandler? Stopped;
    public event EventHandler? Paused;
    public event EventHandler? Unpaused;
}