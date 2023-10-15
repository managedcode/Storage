using System.Threading.Tasks;
using DotNet.Testcontainers.Containers;
using FluentAssertions;
using ManagedCode.Storage.Tests.Common;
using Xunit;

namespace ManagedCode.Storage.Tests.Storages;

public abstract class DownloadTests<T> : BaseContainer<T> where T : IContainer
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