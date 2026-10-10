using System.Threading.Tasks;
using ManagedCode.Storage.Core.Models;
using ManagedCode.Storage.Sftp;
using ManagedCode.Storage.Sftp.Options;
using Shouldly;
using Xunit;

namespace ManagedCode.Storage.Tests.Storages.Sftp;

public sealed class SftpChrootRuntimeContractsTests
{
    [Fact]
    public async Task ChrootCleanup_ReportsTheActualPermissionFailureAndPreservesTheRoot()
    {
        await using var container = SftpContainerFactory.Create();
        var token = TestContext.Current.CancellationToken;
        await container.StartAsync(token);
        using var storage = new SftpStorage(new SftpStorageOptions
        {
            Host = container.GetHost(),
            Port = container.GetPort(),
            Username = SftpContainerFactory.Username,
            Password = SftpContainerFactory.Password,
            RemoteDirectory = "/"
        }, Microsoft.Extensions.Logging.Abstractions.NullLogger<SftpStorage>.Instance);
        (await storage.UploadAsync("nested", new UploadOptions { Directory = "upload/nested/child", FileName = "data.txt" }, token)).IsSuccess.ShouldBeTrue();
        (await storage.UploadAsync("root file", new UploadOptions { Directory = "upload", FileName = "top.txt" }, token)).IsSuccess.ShouldBeTrue();
        (await storage.RemoveContainerAsync(token)).IsFailed.ShouldBeTrue();
        storage.StorageClient.Exists("/").ShouldBeTrue();
        storage.StorageClient.Exists("/upload").ShouldBeTrue();
        storage.StorageClient.Exists("/upload/nested/child/data.txt").ShouldBeFalse();
        storage.StorageClient.Exists("/upload/top.txt").ShouldBeFalse();
    }
}
