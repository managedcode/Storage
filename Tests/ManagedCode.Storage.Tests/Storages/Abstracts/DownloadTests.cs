using System.Threading.Tasks;
using DotNet.Testcontainers.Containers;
using Shouldly;
using ManagedCode.Storage.Tests.Common;
using Xunit;

namespace ManagedCode.Storage.Tests.Storages.Abstracts;

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
        result.IsSuccess
            .ShouldBeTrue();
        result.Value!.FileInfo
            .Length
            .ShouldBe(fileInfo.Length);

        await Storage.DeleteAsync(fileInfo.Name);
    }
}