using ManagedCode.Storage.Tests.Storages.Abstracts;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.Sftp;

using Shouldly;

namespace ManagedCode.Storage.Tests.Storages.Sftp;

/// <summary>
/// Container tests for SFTP storage.
/// </summary>
public class SftpContainerTests : ContainerTests<SftpContainer>
{
    [Xunit.Fact]
    public async System.Threading.Tasks.Task RemoveLogicalContainer_DeletesTheDirectoryAndPreservesTheChrootUploadMount()
    {
        var token = Xunit.TestContext.Current.CancellationToken;
        var client = ((ManagedCode.Storage.Sftp.SftpStorage)Storage).StorageClient;
        (await Storage.CreateContainerAsync(token)).IsSuccess.ShouldBeTrue();
        (await Storage.UploadAsync("nested", o => { o.Directory = "child"; o.FileName = "proof.txt"; }, token)).IsSuccess.ShouldBeTrue();
        (await Storage.RemoveContainerAsync(token)).IsSuccess.ShouldBeTrue();
        client.Exists(SftpContainerFactory.RemoteDirectory).ShouldBeFalse();
        client.Exists("/upload").ShouldBeTrue();
    }

    [Xunit.Theory]
    [Xunit.InlineData(true)]
    [Xunit.InlineData(false)]
    public async System.Threading.Tasks.Task NestedContainerCreation_CreatesAncestorsAndRejectsAnExistingFile(bool autoCreate)
    {
        var token = Xunit.TestContext.Current.CancellationToken;
        var root = SftpContainerFactory.RemoteDirectory + "/parents-" + System.Guid.NewGuid().ToString("N") + "/nested/container";
        var options = new ManagedCode.Storage.Sftp.Options.SftpStorageOptions
        {
            Host = Container.GetHost(),
            Port = Container.GetPort(),
            Username = SftpContainerFactory.Username,
            Password = SftpContainerFactory.Password,
            RemoteDirectory = root,
            CreateContainerIfNotExists = autoCreate
        };
        using var storage = new ManagedCode.Storage.Sftp.SftpStorage(options,
            Microsoft.Extensions.Logging.Abstractions.NullLogger<ManagedCode.Storage.Sftp.SftpStorage>.Instance);
        (await storage.CreateContainerAsync(token)).IsSuccess.ShouldBeTrue();
        (await storage.CreateContainerAsync(token)).IsSuccess.ShouldBeTrue();
        storage.StorageClient.GetAttributes(root).IsDirectory.ShouldBeTrue();
        (await storage.UploadAsync("stored", new ManagedCode.Storage.Core.Models.UploadOptions("proof.txt"), token)).IsSuccess.ShouldBeTrue();
        storage.StorageClient.ReadAllText(root + "/proof.txt").ShouldBe("stored");
        (await storage.RemoveContainerAsync(token)).IsSuccess.ShouldBeTrue();
        storage.StorageClient.Exists(root).ShouldBeFalse();

        (await Storage.UploadAsync("file blocker", new ManagedCode.Storage.Core.Models.UploadOptions("blocker.bin"), token)).IsSuccess.ShouldBeTrue();
        options.RemoteDirectory = SftpContainerFactory.RemoteDirectory + "/blocker.bin";
        using var blocked = new ManagedCode.Storage.Sftp.SftpStorage(options,
            Microsoft.Extensions.Logging.Abstractions.NullLogger<ManagedCode.Storage.Sftp.SftpStorage>.Instance);
        (await blocked.CreateContainerAsync(token)).IsFailed.ShouldBeTrue();
        blocked.StorageClient.GetAttributes(options.RemoteDirectory).IsRegularFile.ShouldBeTrue();
        blocked.StorageClient.ReadAllText(options.RemoteDirectory).ShouldBe("file blocker");
    }

    protected override SftpContainer Build() => SftpContainerFactory.Create();

    protected override ServiceProvider ConfigureServices()
    {
        return SftpConfigurator.ConfigureServices(
            Container.GetHost(),
            Container.GetPort(),
            SftpContainerFactory.Username,
            SftpContainerFactory.Password,
            SftpContainerFactory.RemoteDirectory);
    }
}
