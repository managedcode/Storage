using System;
using System.Linq;
using System.Threading.Tasks;
using DotNet.Testcontainers.Containers;
using Shouldly;
using ManagedCode.Storage.Tests.Common;
using Xunit;

namespace ManagedCode.Storage.Tests.Storages.Abstracts;

public abstract class ContainerTests<T> : BaseContainer<T> where T : IContainer
{
    [Fact]
    public async Task CreateContainer_ShouldBeSuccess()
    {
        var container = await Storage.CreateContainerAsync();
        container.IsSuccess
            .ShouldBeTrue();
    }

    [Fact]
    public async Task CreateContainerAsync()
    {
        await Should.NotThrowAsync(() => Storage.CreateContainerAsync());
    }

    [Fact]
    public async Task RemoveContainer_ShouldBeSuccess()
    {
        var createResult = await Storage.CreateContainerAsync();
        createResult.IsSuccess
            .ShouldBeTrue();

        var result = await Storage.RemoveContainerAsync();

        result.IsSuccess
            .ShouldBeTrue(result.Problem?.Detail ?? "Failed without details");
    }

    [Fact]
    public async Task GetFileListAsyncTest()
    {
        await UploadTestFileAsync();
        await UploadTestFileAsync();
        await UploadTestFileAsync();

        var files = await Storage.GetBlobMetadataListAsync()
            .ToListAsync();
        files.Count
            .ShouldBeGreaterThanOrEqualTo(3);
    }

    [Fact]
    public async Task DeleteDirectory_ShouldBeSuccess()
    {
        // Arrange
        var directory = "test-directory";
        await UploadTestFileListAsync(directory, 3);

        // Act
        var result = await Storage.DeleteDirectoryAsync(directory);
        var blobs = await Storage.GetBlobMetadataListAsync(directory)
            .ToListAsync();

        // Assert
        result.IsSuccess
            .ShouldBeTrue(result.Problem?.Detail ?? "Failed without details");

        blobs.Count
            .ShouldBe(0);
    }
}
