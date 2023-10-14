using System.Threading.Tasks;
using DotNet.Testcontainers.Containers;
using FluentAssertions;
using Xunit;

namespace ManagedCode.Storage.Tests;

public abstract class DownloadTests<T> : BaseContainer<T> where T : DockerContainer
{
    [Fact]
    public async Task DownloadAsync_WithoutOptions_AsLocalFile()
    {
        // Arrange
        var fileInfo = await UploadTestFileAsync();

        // Act
        var result = await Storage.DownloadAsync(fileInfo.Name);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.FileInfo.Length.Should().Be(fileInfo.Length);

        await Storage.DeleteAsync(fileInfo.Name);
    }
}