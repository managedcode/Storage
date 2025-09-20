using System;
using System.Linq;
using System.Threading.Tasks;
using DotNet.Testcontainers.Containers;
using Shouldly;
using ManagedCode.Storage.Core.Models;
using ManagedCode.Storage.Tests.Common;
using Xunit;

namespace ManagedCode.Storage.Tests.Storages.Abstracts;

public abstract class BlobTests<T> : BaseContainer<T> where T : IContainer
{
    [Fact]
    public async Task GetBlobListAsync_WithoutOptions()
    {
        // Arrange
        var fileList = await UploadTestFileListAsync();

        // Act
        var result = await Storage.GetBlobMetadataListAsync()
            .ToListAsync();

        // Assert
        result.Count
            .ShouldBe(fileList.Count);

        foreach (var item in fileList)
        {
            var file = result.FirstOrDefault(f => f.Name == item.Name);
            file.ShouldNotBeNull();

            await Storage.DeleteAsync(item.Name);
        }
    }

    [Fact]
    public virtual async Task GetBlobMetadataAsync_ShouldBeTrue()
    {
        // Arrange
        var fileInfo = await UploadTestFileAsync();

        // Act
        var result = await Storage.GetBlobMetadataAsync(fileInfo.Name);

        // Assert
        result.IsSuccess
            .ShouldBeTrue();
        result.Value!.Length
            .ShouldBe((ulong)fileInfo.Length);
        result.Value!.Name
            .ShouldBe(fileInfo.Name);

        await Storage.DeleteAsync(fileInfo.Name);
    }

    [Fact]
    public async Task DeleteAsync_WithoutOptions_ShouldTrue()
    {
        // Arrange
        var file = await UploadTestFileAsync();

        // Act
        var result = await Storage.DeleteAsync(file.Name);

        // Assert
        result.IsSuccess
            .ShouldBeTrue();
        result.Value
            .ShouldBeTrue();
    }

    [Fact]
    public async Task DeleteAsync_WithoutOptions_IfFileDontExist_ShouldFalse()
    {
        // Arrange
        var blob = Guid.NewGuid()
            .ToString();

        // Act
        var result = await Storage.DeleteAsync(blob);

        // Assert
        result.IsSuccess
            .ShouldBeTrue();
        result.Value
            .ShouldBeFalse();
    }

    [Fact]
    public async Task DeleteAsync_WithOptions_FromDirectory_ShouldTrue()
    {
        // Arrange
        var directory = "test-directory";
        var file = await UploadTestFileAsync(directory);
        DeleteOptions options = new() { FileName = file.Name, Directory = directory };

        // Act
        var result = await Storage.DeleteAsync(options);

        // Assert
        result.IsSuccess
            .ShouldBeTrue();
        result.Value
            .ShouldBeTrue();
    }

    [Fact]
    public async Task DeleteAsync_WithOptions_IfFileDontExist_FromDirectory_ShouldFalse()
    {
        // Arrange
        var directory = "test-directory";
        DeleteOptions options = new()
        {
            FileName = Guid.NewGuid()
                .ToString(),
            Directory = directory
        };

        // Act
        var result = await Storage.DeleteAsync(options);

        // Assert
        result.IsSuccess
            .ShouldBeTrue();
        result.Value
            .ShouldBeFalse();
    }

    [Fact]
    public async Task ExistsAsync_WithoutOptions_ShouldBeTrue()
    {
        // Arrange
        var fileInfo = await UploadTestFileAsync();

        // Act
        var result = await Storage.ExistsAsync(fileInfo.Name);

        // Assert
        result.IsSuccess
            .ShouldBeTrue();
        result.Value
            .ShouldBeTrue();

        await Storage.DeleteAsync(fileInfo.Name);
    }

    [Fact]
    public async Task ExistsAsync_WithOptions_InDirectory_ShouldBeTrue()
    {
        // Arrange
        var directory = "test-directory";
        var fileInfo = await UploadTestFileAsync(directory);
        ExistOptions options = new() { FileName = fileInfo.Name, Directory = directory };

        // Act
        var result = await Storage.ExistsAsync(options);

        // Assert
        result.IsSuccess
            .ShouldBeTrue();
        result.Value
            .ShouldBeTrue();

        await Storage.DeleteAsync(fileInfo.Name);
    }

    [Fact]
    public async Task ExistsAsync_IfFileDontExist_WithoutOptions_ShouldBeFalse()
    {
        // Act
        var result = await Storage.ExistsAsync(Guid.NewGuid()
            .ToString());

        // Assert
        result.IsSuccess
            .ShouldBeTrue();
        result.Value
            .ShouldBeFalse();
    }

    [Fact]
    public async Task ExistsAsync_IfFileFileExistInAnotherDirectory_WithOptions_ShouldBeFalse()
    {
        // Arrange
        var directory = "test-directory";
        var fileInfo = await UploadTestFileAsync(directory);
        ExistOptions options = new() { FileName = fileInfo.Name, Directory = "another-directory" };

        // Act
        var result = await Storage.ExistsAsync(options);

        // Assert
        result.IsSuccess
            .ShouldBeTrue();
        result.Value
            .ShouldBeFalse();

        await Storage.DeleteAsync(fileInfo.Name);
    }
}