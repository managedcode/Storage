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

namespace ManagedCode.Storage.Tests.Common;

public sealed class EmptyContainer : IContainer
{
    public ValueTask DisposeAsync()
    {
        return ValueTask.CompletedTask;
    }

    public ushort GetMappedPublicPort(int containerPort)
    {
        throw new NotImplementedException();
    }

    public ushort GetMappedPublicPort(string containerPort)
    {
        throw new NotImplementedException();
    }

    public Task<long> GetExitCodeAsync(CancellationToken ct = new())
    {
        throw new NotImplementedException();
    }

    public Task<(string Stdout, string Stderr)> GetLogsAsync(DateTime since = new(), DateTime until = new(), bool timestampsEnabled = true,
        CancellationToken ct = new())
    {
        throw new NotImplementedException();
    }

    public Task StartAsync(CancellationToken ct = new())
    {
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken ct = new())
    {
        throw new NotImplementedException();
    }

    public Task CopyAsync(byte[] fileContent, string filePath,
        UnixFileModes fileMode = UnixFileModes.None | UnixFileModes.OtherRead | UnixFileModes.GroupRead | UnixFileModes.UserWrite |
                                 UnixFileModes.UserRead, CancellationToken ct = new())
    {
        throw new NotImplementedException();
    }

    public Task CopyAsync(string source, string target,
        UnixFileModes fileMode = UnixFileModes.None | UnixFileModes.OtherRead | UnixFileModes.GroupRead | UnixFileModes.UserWrite |
                                 UnixFileModes.UserRead, CancellationToken ct = new())
    {
        throw new NotImplementedException();
    }

    public Task CopyAsync(DirectoryInfo source, string target,
        UnixFileModes fileMode = UnixFileModes.None | UnixFileModes.OtherRead | UnixFileModes.GroupRead | UnixFileModes.UserWrite |
                                 UnixFileModes.UserRead, CancellationToken ct = new())
    {
        throw new NotImplementedException();
    }

    public Task CopyAsync(FileInfo source, string target,
        UnixFileModes fileMode = UnixFileModes.None | UnixFileModes.OtherRead | UnixFileModes.GroupRead | UnixFileModes.UserWrite |
                                 UnixFileModes.UserRead, CancellationToken ct = new())
    {
        throw new NotImplementedException();
    }

    public Task<byte[]> ReadFileAsync(string filePath, CancellationToken ct = new())
    {
        throw new NotImplementedException();
    }

    public Task<ExecResult> ExecAsync(IList<string> command, CancellationToken ct = new())
    {
        throw new NotImplementedException();
    }

    public DateTime CreatedTime { get; }
    public DateTime StartedTime { get; }
    public DateTime StoppedTime { get; }

    public ILogger Logger { get; } = NullLogger.Instance;
    public string Id { get; } = "none";
    public string Name { get; } = "none";
    public string IpAddress { get; } = "none";
    public string MacAddress { get; } = "none";
    public string Hostname { get; } = "none";
    public IImage Image { get; } = new DockerImage("none");
    public TestcontainersStates State { get; } = TestcontainersStates.Running;
    public TestcontainersHealthStatus Health { get; } = TestcontainersHealthStatus.Healthy;
    public long HealthCheckFailingStreak { get; } = 0;
    public event EventHandler? Creating;
    public event EventHandler? Starting;
    public event EventHandler? Stopping;
    public event EventHandler? Created;
    public event EventHandler? Started;
    public event EventHandler? Stopped;
}