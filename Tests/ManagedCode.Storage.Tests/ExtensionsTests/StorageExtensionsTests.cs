using System;
using System.IO;
using System.Threading.Tasks;
using Shouldly;
using ManagedCode.MimeTypes;
using ManagedCode.Storage.Core;
using ManagedCode.Storage.Core.Models;
using ManagedCode.Storage.FileSystem;
using ManagedCode.Storage.FileSystem.Extensions;
using ManagedCode.Storage.Server;
using ManagedCode.Storage.Server.Extensions.Storage;
using ManagedCode.Storage.Tests.Common;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace ManagedCode.Storage.Tests.ExtensionsTests;

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
        localFile.IsSuccess.ShouldBeTrue();
        var downloaded = localFile.Value ?? throw new InvalidOperationException("Download result is missing a file");
        downloaded.FileInfo.Length.ShouldBe(formFile.Length);
        downloaded.Name.ShouldBe(formFile.FileName);

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
        var uploadResult = await Storage.UploadToStorageAsync(formFile);
        uploadResult.IsSuccess.ShouldBeTrue();
        var result = await Storage.DownloadAsync(fileName);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        var downloaded = result.Value ?? throw new InvalidOperationException("Download result is missing a file");
        downloaded.Name.ShouldBe(formFile.FileName);

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
        result.IsSuccess.ShouldBeTrue();
        var uploaded = result.Value ?? throw new InvalidOperationException("Upload result is missing metadata");
        var localFile = await Storage.DownloadAsync(uploaded.Name);

        // Assert
        localFile.IsSuccess.ShouldBeTrue();
        var downloaded = localFile.Value ?? throw new InvalidOperationException("Download result is missing a file");
        downloaded.FileInfo.Length.ShouldBe(formFile.Length);
        downloaded.Name.ShouldBe(fileName);

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
        var uploadResult = await Storage.UploadAsync(localFile.FileInfo);
        uploadResult.IsSuccess.ShouldBeTrue();
        var result = await Storage.DownloadAsFileResult(fileName);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        var fileResult = result.Value ?? throw new InvalidOperationException("Download result is missing file info");
        fileResult.ContentType.ShouldBe(MimeHelper.GetMimeType(localFile.FileInfo.Extension));
        fileResult.FileDownloadName.ShouldBe(localFile.Name);

        await Storage.DeleteAsync(fileName);
    }

    [Fact]
    public async Task DownloadAsFileResult_WithBlobMetadata()
    {
        // Arrange
        const int size = 200 * 1024; // 200 KB
        var fileName = FileHelper.GenerateRandomFileName();
        var localFile = FileHelper.GenerateLocalFile(fileName, size);

        // Act
        await Storage.UploadAsync(localFile.FileInfo, options => { options.FileName = fileName; });
        var result = await Storage.DownloadAsFileResult(fileName);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        var fileResult = result.Value ?? throw new InvalidOperationException("Download result is missing file info");
        fileResult.ContentType.ShouldBe(MimeHelper.GetMimeType(localFile.FileInfo.Extension));
        fileResult.FileDownloadName.ShouldBe(localFile.Name);

        await Storage.DeleteAsync(fileName);
    }

    [Fact]
    public async Task DownloadAsFileResult_WithFileName_IfFileDontExist()
    {
        // Arrange
        var fileName = FileHelper.GenerateRandomFileName();

        // Act
        var fileResult = await Storage.DownloadAsFileResult(fileName);

        // Assert
        fileResult.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    public async Task DownloadAsFileResult_WithBlobMetadata_IfFileDontExist()
    {
        // Arrange
        var fileName = FileHelper.GenerateRandomFileName();

        BlobMetadata blobMetadata = new() { Name = fileName };

        // Act
        var fileResult = await Storage.DownloadAsFileResult(blobMetadata);

        // Assert
        fileResult.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    public void MultipleStorages_WithDifferentKeys()
    {
        // Arrange
        var services = new ServiceCollection();

        services.AddFileSystemStorage("storage1", opt =>
        {
            opt.BaseFolder = Path.Combine(Environment.CurrentDirectory, "managed-code-bucket-1");
        });

        services.AddFileSystemStorage("storage2", opt =>
        {
            opt.BaseFolder = Path.Combine(Environment.CurrentDirectory, "managed-code-bucket-2");
        });

        var provider = services.BuildServiceProvider();

        // Act
        var storage1 = provider.GetKeyedService<IFileSystemStorage>("storage1");
        var storage2 = provider.GetKeyedService<IFileSystemStorage>("storage2");

        // Assert
        storage1.ShouldNotBeNull();
        storage2.ShouldNotBeNull();
        storage1.ShouldNotBeSameAs(storage2);
    }
}
