using System;
using System.Linq;
using System.Threading.Tasks;
using DotNet.Testcontainers.Containers;
using FluentAssertions;
using Xunit;

namespace ManagedCode.Storage.Tests;

public abstract class ContainerTests<T> : BaseContainer<T> where T : DockerContainer
{
    
    [Fact]
    public async Task CreateContainer_ShouldBeSuccess()
    {
        var container = await Storage.CreateContainerAsync();
        container.IsSuccess.Should().BeTrue();
    }
    
    [Fact]
    public async Task CreateContainerAsync()
    {
        await FluentActions.Awaiting(() => Storage.CreateContainerAsync())
            .Should()
            .NotThrowAsync<Exception>();
    }
    
    [Fact(Skip = "Other tests fail because container removal is too slow")]
    public async Task RemoveContainer_ShouldBeSuccess()
    {
        var createResult = await Storage.CreateContainerAsync();
        createResult.IsSuccess.Should().BeTrue();

        var result = await Storage.RemoveContainerAsync();

        result.IsSuccess.Should().BeTrue();
    }
    
    [Fact]
    public async Task GetFileListAsyncTest()
    {
        await StreamUploadAsyncTest();
        await ArrayUploadAsyncTest();
        await StringUploadAsyncTest();

        var files = await Storage.GetBlobMetadataListAsync().ToListAsync();
        files.Count.Should().BeGreaterOrEqualTo(3);
    }

    [Fact]
    public async Task DeleteDirectory_ShouldBeSuccess()
    {
        // Arrange
        var directory = "test-directory";
        await UploadTestFileListAsync(directory, 3);

        // Act
        var result = await Storage.DeleteDirectoryAsync(directory);
        var blobs = await Storage.GetBlobMetadataListAsync(directory).ToListAsync();

        // Assert
        result.IsSuccess.Should().BeTrue();
        blobs.Count.Should().Be(0);
    }
}