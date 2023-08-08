using System;
using System.IO;
using System.Threading.Tasks;
using FluentAssertions;
using ManagedCode.MimeTypes;
using ManagedCode.Storage.AspNetExtensions;
using ManagedCode.Storage.Core;
using ManagedCode.Storage.Core.Models;
using ManagedCode.Storage.FileSystem.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace ManagedCode.Storage.Tests.AspNetExtensions;

public class StorageExtensionsTests
{
    public StorageExtensionsTests()
    {
        ServiceProvider = ConfigureServices();
        Storage = ServiceProvider.GetService<IStorage>()!;
    }

    public IStorage Storage { get; }
    public ServiceProvider ServiceProvider { get; }

    public static ServiceProvider ConfigureServices()
    {
        var services = new ServiceCollection();

        services.AddFileSystemStorageAsDefault(opt => { opt.BaseFolder = Path.Combine(Environment.CurrentDirectory, "managed-code-bucket"); });

        return services.BuildServiceProvider();
    }

    [Fact]
    public async Task UploadToStorageAsync_SmallFile()
    {
        // Arrange
        const int size = 200 * 1024; // 200 KB
        var fileName = FileHelper.GenerateRandomFileName();
        var formFile = FileHelper.GenerateFormFile(fileName, size);

        // Act
        await Storage.UploadToStorageAsync(formFile);
        var localFile = await Storage.DownloadAsync(fileName);

        // Assert
        localFile!.Value.FileInfo.Length.Should().Be(formFile.Length);
        localFile.Value.Name.Should().Be(formFile.FileName);

        await Storage.DeleteAsync(fileName);
    }

    [Fact]
    public async Task UploadToStorageAsync_LargeFile()
    {
        // Arrange
        const int size = 50 * 1024 * 1024; // 50 MB
        var fileName = FileHelper.GenerateRandomFileName();
        var formFile = FileHelper.GenerateFormFile(fileName, size);

        // Act
        var res = await Storage.UploadToStorageAsync(formFile);
        var result = await Storage.DownloadAsync(fileName);

        // Assert
        result.Value.Name.Should().Be(formFile.FileName);

        await Storage.DeleteAsync(fileName);
    }

    [Fact]
    public async Task UploadToStorageAsync_WithRandomName()
    {
        // Arrange
        const int size = 200 * 1024; // 200 KB
        var fileName = FileHelper.GenerateRandomFileName();
        var formFile = FileHelper.GenerateFormFile(fileName, size);

        // Act
        var result = await Storage.UploadToStorageAsync(formFile);
        var localFile = await Storage.DownloadAsync(result.Value.Name);

        // Assert
        result.IsSuccess.Should().BeTrue();
        localFile.Value.FileInfo.Length.Should().Be(formFile.Length);
        localFile.Value.Name.Should().Be(fileName);

        await Storage.DeleteAsync(fileName);
    }

    [Fact]
    public async Task DownloadAsFileResult_WithFileName()
    {
        // Arrange
        const int size = 200 * 1024; // 200 KB
        var fileName = FileHelper.GenerateRandomFileName();
        var localFile = FileHelper.GenerateLocalFile(fileName, size);

        // Act
        await Storage.UploadAsync(localFile.FileInfo);
        var result = await Storage.DownloadAsFileResultAsync(fileName);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.ContentType.Should().Be(MimeHelper.GetMimeType(localFile.FileInfo.Extension));
        result.Value.FileDownloadName.Should().Be(localFile.Name);

        await Storage.DeleteAsync(fileName);
    }

    [Fact]
    public async Task DownloadAsFileResult_WithBlobMetadata()
    {
        // Arrange
        const int size = 200 * 1024; // 200 KB
        var fileName = FileHelper.GenerateRandomFileName();
        var localFile = FileHelper.GenerateLocalFile(fileName, size);

        BlobMetadata blobMetadata = new() { Name = fileName };

        // Act
        await Storage.UploadAsync(localFile.FileInfo, options => { options.FileName = fileName; });
        var result = await Storage.DownloadAsFileResultAsync(fileName);

        // Assert
        result.IsSuccess.Should().Be(true);
        result.Value!.ContentType.Should().Be(MimeHelper.GetMimeType(localFile.FileInfo.Extension));
        result.Value.FileDownloadName.Should().Be(localFile.Name);

        await Storage.DeleteAsync(fileName);
    }

    [Fact]
    public async Task DownloadAsFileResult_WithFileName_IfFileDontExist()
    {
        // Arrange
        var fileName = FileHelper.GenerateRandomFileName();

        // Act
        var fileResult = await Storage.DownloadAsFileResultAsync(fileName);

        // Assert
        fileResult.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task DownloadAsFileResult_WithBlobMetadata_IfFileDontExist()
    {
        // Arrange
        var fileName = FileHelper.GenerateRandomFileName();

        BlobMetadata blobMetadata = new() { Name = fileName };

        // Act
        var fileResult = await Storage.DownloadAsFileResultAsync(blobMetadata);

        // Assert
        fileResult.IsSuccess.Should().BeFalse();
    }
}