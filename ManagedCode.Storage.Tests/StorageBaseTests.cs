using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using ManagedCode.Storage.Core;
using ManagedCode.Storage.Core.Models;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

// ReSharper disable MethodHasAsyncOverload

namespace ManagedCode.Storage.Tests;

public abstract class StorageBaseTests
{
    protected IStorage Storage { get; }
    protected ServiceProvider ServiceProvider { get; }

    protected abstract ServiceProvider ConfigureServices();

    protected StorageBaseTests()
    {
        ServiceProvider = ConfigureServices();
        Storage = ServiceProvider.GetService<IStorage>()!;
    }

    [Fact]
    public async Task CreateContainer_ShouldBeSuccess()
    {
        var container = await Storage.CreateContainerAsync();
        container.IsSuccess.Should().BeTrue();
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
    public async Task StreamUploadAsyncTest()
    {
        var file = await GetTestFileAsync();
        var uploadResult = await Storage.UploadAsync(file.OpenRead());
        uploadResult.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task ArrayUploadAsyncTest()
    {
        var file = await GetTestFileAsync();
        var bytes = await File.ReadAllBytesAsync(file.FullName);
        var uploadResult = await Storage.UploadAsync(bytes);
        uploadResult.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task StringUploadAsyncTest()
    {
        var file = await GetTestFileAsync();
        var text = await File.ReadAllTextAsync(file.FullName);
        var uploadResult = await Storage.UploadAsync(text);
        uploadResult.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task FileInfoUploadAsyncTest()
    {
        var file = await GetTestFileAsync();
        var uploadResult = await Storage.UploadAsync(file);
        uploadResult.IsSuccess.Should().BeTrue();

        var downloadResult = await Storage.DownloadAsync(uploadResult.Value!.Name);
        downloadResult.IsSuccess.Should().BeTrue();
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


    #region MemoryPayload

// [Fact]
// public async Task UploadBigFilesAsync()
// {
//     const int fileSize = 70 * 1024 * 1024;
//
//     var bigFiles = new List<LocalFile>()
//     {
//         GetLocalFile(fileSize),
//         GetLocalFile(fileSize),
//         GetLocalFile(fileSize)
//     };
//
//     foreach (var localFile in bigFiles)
//     {
//         await Storage.UploadStreamAsync(localFile.FileName, localFile.FileStream);
//         await localFile.DisposeAsync();
//     }
//
//     Process currentProcess = Process.GetCurrentProcess();
//     long totalBytesOfMemoryUsed = currentProcess.WorkingSet64;
//
//     totalBytesOfMemoryUsed.Should().BeLessThan(3 * fileSize);
//
//     foreach (var localFile in bigFiles)
//     {
//         await Storage.DeleteAsync(localFile.FileName);
//     }
// }

    #endregion


    #region Get

    [Fact]
    public async Task GetBlobListAsync_WithoutOptions()
    {
        // Arrange
        var fileList = await UploadTestFileListAsync();

        // Act
        var result = await Storage.GetBlobMetadataListAsync().ToListAsync();

        // Assert
        result.Count.Should().BeGreaterThan(fileList.Count);

        foreach (var item in fileList)
        {
            var file = result.FirstOrDefault(f => f.Name == item.Name);
            file.Should().NotBeNull();

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
        result.IsSuccess.Should().BeTrue();
        result.Value!.Length.Should().Be(fileInfo.Length);
        result.Value!.Name.Should().Be(fileInfo.Name);

        await Storage.DeleteAsync(fileInfo.Name);
    }

    #endregion

    [Fact]
    public async Task DeleteDirectory_ShouldBeSuccess()
    {
        // Arrange
        var directory = "test-directory";
        await UploadTestFileListAsync(directory);

        // Act
        var result = await Storage.DeleteDirectoryAsync(directory);
        var blobs = await Storage.GetBlobMetadataListAsync(directory).ToListAsync();


        // Assert
        result.IsSuccess.Should().BeTrue();
        blobs.Count.Should().Be(0);
    }

    #region Upload

    [Fact]
    public async Task UploadAsync_AsText_WithoutOptions()
    {
        // Arrange
        var uploadContent = FileHelper.GenerateRandomFileContent();

        // Act
        var result = await Storage.UploadAsync(uploadContent);
        var downloadedResult = await Storage.DownloadAsync(result.Value!.Name);

        // Assert
        result.IsSuccess.Should().BeTrue();
        downloadedResult.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task UploadAsync_AsStream_WithoutOptions()
    {
        // Arrange
        var uploadContent = FileHelper.GenerateRandomFileContent();
        var byteArray = Encoding.ASCII.GetBytes(uploadContent);
        var stream = new MemoryStream(byteArray);

        // Act
        var result = await Storage.UploadAsync(stream);
        var downloadedResult = await Storage.DownloadAsync(result.Value!.Name);

        // Assert
        result.IsSuccess.Should().BeTrue();
        downloadedResult.IsSuccess.Should().BeTrue();
    }

    #region Directory

    [Fact]
    public async Task UploadAsync_AsStream_WithOptions_ToDirectory_SpecifyingFileName()
    {
        // Arrange
        var directory = "test-directory";
        var uploadContent = FileHelper.GenerateRandomFileContent();
        var fileName = FileHelper.GenerateRandomFileName();

        var byteArray = Encoding.ASCII.GetBytes(uploadContent);
        var stream = new MemoryStream(byteArray);

        // Act
        var result = await Storage.UploadAsync(stream, new UploadOptions() {FileName = fileName, Directory = directory});
        var downloadedResult = await Storage.DownloadAsync(new DownloadOptions() {FileName = fileName, Directory = directory});

        // Assert
        result.IsSuccess.Should().BeTrue();
        downloadedResult.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task UploadAsync_AsArray_WithOptions_ToDirectory_SpecifyingFileName()
    {
        // Arrange
        var directory = "test-directory";
        var uploadContent = FileHelper.GenerateRandomFileContent();
        var fileName = FileHelper.GenerateRandomFileName();

        var byteArray = Encoding.ASCII.GetBytes(uploadContent);

        // Act
        var result = await Storage.UploadAsync(byteArray, new UploadOptions() {FileName = fileName, Directory = directory});
        var downloadedResult = await Storage.DownloadAsync(new DownloadOptions() {FileName = fileName, Directory = directory});

        // Assert
        result.IsSuccess.Should().BeTrue();
        downloadedResult.IsSuccess.Should().BeTrue();

        await Storage.DeleteAsync(fileName);
    }

    [Fact]
    public async Task UploadAsync_AsText_WithOptions_ToDirectory_SpecifyingFileName()
    {
        // Arrange
        var directory = "test-directory";
        var uploadContent = FileHelper.GenerateRandomFileContent();
        var fileName = FileHelper.GenerateRandomFileName();

        // Act
        var result = await Storage.UploadAsync(uploadContent, new UploadOptions() {FileName = fileName, Directory = directory});
        var downloadedResult = await Storage.DownloadAsync(new DownloadOptions() {FileName = fileName, Directory = directory});

        // Assert
        result.IsSuccess.Should().BeTrue();
        downloadedResult.IsSuccess.Should().BeTrue();

        await Storage.DeleteAsync(fileName);
    }

    #endregion

    #endregion

    #region Download

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

    #endregion

    #region Exist

    [Fact]
    public async Task ExistsAsync_WithoutOptions_ShouldBeTrue()
    {
        // Arrange
        var fileInfo = await UploadTestFileAsync();

        // Act
        var result = await Storage.ExistsAsync(fileInfo.Name);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeTrue();

        await Storage.DeleteAsync(fileInfo.Name);
    }


    [Fact]
    public async Task ExistsAsync_WithOptions_InDirectory_ShouldBeTrue()
    {
        // Arrange
        var directory = "test-directory";
        var fileInfo = await UploadTestFileAsync(directory);
        ExistOptions options = new() {FileName = fileInfo.Name, Directory = directory};

        // Act
        var result = await Storage.ExistsAsync(options);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeTrue();

        await Storage.DeleteAsync(fileInfo.Name);
    }

    [Fact]
    public async Task ExistsAsync_IfFileDontExist_WithoutOptions_ShouldBeFalse()
    {
        // Act
        var result = await Storage.ExistsAsync(Guid.NewGuid().ToString());

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeFalse();
    }


    [Fact]
    public async Task ExistsAsync_IfFileFileExistInAnotherDirectory_WithOptions_ShouldBeFalse()
    {
        // Arrange
        var directory = "test-directory";
        var fileInfo = await UploadTestFileAsync(directory);
        ExistOptions options = new() {FileName = fileInfo.Name, Directory = "another-directory"};

        // Act
        var result = await Storage.ExistsAsync(options);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeFalse();

        await Storage.DeleteAsync(fileInfo.Name);
    }

    #endregion

    #region Delete

    [Fact]
    public async Task DeleteAsync_WithoutOptions_ShouldTrue()
    {
        // Arrange
        var file = await UploadTestFileAsync();

        // Act
        var result = await Storage.DeleteAsync(file.Name);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeTrue();
    }


    [Fact]
    public async Task DeleteAsync_WithoutOptions_IfFileDontExist_ShouldFalse()
    {
        // Arrange
        var blob = Guid.NewGuid().ToString();

        // Act
        var result = await Storage.DeleteAsync(blob);

        // Assert
        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task DeleteAsync_WithOptions_FromDirectory_ShouldTrue()
    {
        // Arrange
        var directory = "test-directory";
        var file = await UploadTestFileAsync(directory);
        DeleteOptions options = new() {FileName = file.Name, Directory = directory};

        // Act
        var result = await Storage.DeleteAsync(options);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeTrue();
    }

    [Fact]
    public async Task DeleteAsync_WithOptions_IfFileDontExist_FromDirectory_ShouldFalse()
    {
        // Arrange
        var directory = "test-directory";
        DeleteOptions options = new() {FileName = Guid.NewGuid().ToString(), Directory = directory};

        // Act
        var result = await Storage.DeleteAsync(options);

        // Assert
        result.IsSuccess.Should().BeFalse();
    }

    #endregion

    #region CreateContainer

    [Fact]
    public async Task CreateContainerAsync()
    {
        await FluentActions.Awaiting(() => Storage.CreateContainerAsync())
            .Should().NotThrowAsync<Exception>();
    }

    #endregion

    private async Task<FileInfo> UploadTestFileAsync(string? directory = null)
    {
        var file = await GetTestFileAsync();

        UploadOptions options = new() {FileName = file.Name, Directory = directory};
        var result = await Storage.UploadAsync(file.OpenRead(), options);
        result.IsSuccess.Should().BeTrue();

        return file;
    }

    private async Task<List<FileInfo>> UploadTestFileListAsync(string? directory = null, int? count = 10)
    {
        List<FileInfo> fileList = new();

        for (var i = 0; i < count; i++)
        {
            var file = await UploadTestFileAsync(directory);
            fileList.Add(file);
        }

        return fileList;
    }

    protected async Task<FileInfo> GetTestFileAsync()
    {
        var fileName = Path.GetTempFileName();
        var fs = File.OpenWrite(fileName);
        var sw = new StreamWriter(fs);

        for (var i = 0; i < 1000; i++)
        {
            await sw.WriteLineAsync(Guid.NewGuid().ToString());
        }

        await sw.DisposeAsync();
        await fs.DisposeAsync();

        return new FileInfo(fileName);
    }
}