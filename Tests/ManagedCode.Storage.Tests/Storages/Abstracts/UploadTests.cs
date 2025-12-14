using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using DotNet.Testcontainers.Containers;
using Shouldly;
using ManagedCode.MimeTypes;
using ManagedCode.Storage.Core.Models;
using ManagedCode.Storage.FileSystem;
using ManagedCode.Storage.Tests.Common;
using Xunit;

// ReSharper disable MethodHasAsyncOverload

namespace ManagedCode.Storage.Tests.Storages.Abstracts;

public abstract class UploadTests<T> : BaseContainer<T> where T : IContainer
{
    [Fact]
    public async Task UploadAsync_AsText_WithoutOptions()
    {
        // Arrange
        var uploadContent = FileHelper.GenerateRandomFileContent();

        // Act
        var result = await Storage.UploadAsync(uploadContent);

        // Assert
        result.IsSuccess
            .ShouldBeTrue();

        var downloadedResult = await Storage.DownloadAsync(result.Value!.Name);
        downloadedResult.IsSuccess
            .ShouldBeTrue();
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

        // Assert
        result.IsSuccess
            .ShouldBeTrue();

        var downloadedResult = await Storage.DownloadAsync(result.Value!.Name);
        downloadedResult.IsSuccess
            .ShouldBeTrue();
    }

    [Fact]
    public async Task StreamUploadAsyncTest()
    {
        var file = await GetTestFileAsync();
        var uploadResult = await Storage.UploadAsync(file.OpenRead());
        uploadResult.IsSuccess
            .ShouldBeTrue();
    }

    [Fact]
    public async Task ArrayUploadAsyncTest()
    {
        var file = await GetTestFileAsync();
        var bytes = await File.ReadAllBytesAsync(file.FullName);
        var uploadResult = await Storage.UploadAsync(bytes);
        uploadResult.IsSuccess
            .ShouldBeTrue();
    }

    [Fact]
    public async Task StringUploadAsyncTest()
    {
        var file = await GetTestFileAsync();
        var text = await File.ReadAllTextAsync(file.FullName);
        var uploadResult = await Storage.UploadAsync(text);
        uploadResult.IsSuccess
            .ShouldBeTrue();
    }

    [Fact]
    public async Task FileInfoUploadAsyncTest()
    {
        var file = await GetTestFileAsync();
        var uploadResult = await Storage.UploadAsync(file);
        uploadResult.IsSuccess
            .ShouldBeTrue();

        var downloadResult = await Storage.DownloadAsync(uploadResult.Value!.Name);
        downloadResult.IsSuccess
            .ShouldBeTrue();
    }

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
        var result = await Storage.UploadAsync(stream, new UploadOptions { FileName = fileName, Directory = directory });
        var downloadedResult = await Storage.DownloadAsync(new DownloadOptions { FileName = fileName, Directory = directory });

        // Assert
        result.IsSuccess
            .ShouldBeTrue();
        downloadedResult.IsSuccess
            .ShouldBeTrue();
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
        var result = await Storage.UploadAsync(byteArray, new UploadOptions { FileName = fileName, Directory = directory });
        var downloadedResult = await Storage.DownloadAsync(new DownloadOptions { FileName = fileName, Directory = directory });

        // Assert
        result.IsSuccess
            .ShouldBeTrue();
        downloadedResult.IsSuccess
            .ShouldBeTrue();

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
        var result = await Storage.UploadAsync(uploadContent, new UploadOptions { FileName = fileName, Directory = directory });
        var downloadedResult = await Storage.DownloadAsync(new DownloadOptions { FileName = fileName, Directory = directory });

        // Assert
        result.IsSuccess
            .ShouldBeTrue();
        downloadedResult.IsSuccess
            .ShouldBeTrue();

        await Storage.DeleteAsync(fileName);
    }

    [Theory]
    [Trait("Category", "LargeFile")]
    [InlineData(1)]
    [InlineData(3)]
    [InlineData(5)]
    public virtual async Task UploadAsync_LargeStream_ShouldRoundTrip(int gigabytes)
    {
        var sizeBytes = LargeFileTestHelper.ResolveSizeBytes(gigabytes);
        var directory = "large-files";

        var containerResult = await Storage.CreateContainerAsync(CancellationToken.None);
        containerResult.IsSuccess.ShouldBeTrue();

        await using var localFile = await LargeFileTestHelper.CreateRandomFileAsync(sizeBytes, ".bin");
        var expectedCrc = LargeFileTestHelper.CalculateFileCrc(localFile.FilePath);
        var fileName = Path.GetFileName(localFile.FilePath);

        var uploadOptions = new UploadOptions
        {
            FileName = fileName,
            Directory = directory,
            MimeType = MimeHelper.GetMimeType(fileName)
        };

        string directoryPath = uploadOptions.Directory ?? string.Empty;
        string storedName = uploadOptions.FileName;

        await using (var uploadStream = File.OpenRead(localFile.FilePath))
        {
            var uploadResult = await Storage.UploadAsync(uploadStream, uploadOptions, CancellationToken.None);
            uploadResult.IsSuccess.ShouldBeTrue();
            uploadResult.Value.ShouldNotBeNull();

            if (!string.IsNullOrWhiteSpace(uploadResult.Value!.FullName))
            {
                var full = uploadResult.Value.FullName!;
                var slashIndex = full.LastIndexOf('/');
                if (slashIndex >= 0)
                {
                    directoryPath = full[..slashIndex];
                    storedName = full[(slashIndex + 1)..];
                }
                else
                {
                    directoryPath = string.Empty;
                    storedName = full;
                }
            }
            else if (!string.IsNullOrWhiteSpace(uploadResult.Value.Name))
            {
                storedName = uploadResult.Value.Name!;
            }
        }

        var downloadResult = await Storage.DownloadAsync(new DownloadOptions
        {
            FileName = storedName,
            Directory = string.IsNullOrWhiteSpace(directoryPath) ? null : directoryPath
        }, CancellationToken.None);

        downloadResult.IsSuccess.ShouldBeTrue();
        downloadResult.Value.ShouldNotBeNull();

        await using var downloaded = downloadResult.Value!;
        var actualCrc = LargeFileTestHelper.CalculateFileCrc(downloaded.FilePath);
        actualCrc.ShouldBe(expectedCrc);

        var deleteResult = await Storage.DeleteAsync(new DeleteOptions
        {
            FileName = storedName,
            Directory = string.IsNullOrWhiteSpace(directoryPath) ? null : directoryPath
        }, CancellationToken.None);

        deleteResult.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public async Task UploadAsync_WithCancellationToken_ShouldCancel()
    {
        // Arrange
        var uploadContent = FileHelper.GenerateRandomFileContent();
        var byteArray = Encoding.ASCII.GetBytes(uploadContent);
        var stream = new MemoryStream(byteArray);
        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act
        var result = await Storage.UploadAsync(stream, cancellationToken: cts.Token);

        // Assert
        result.IsSuccess
            .ShouldBeFalse();
    }
    
    
    [Fact]
    public virtual async Task UploadAsync_WithCancellationToken_BigFile_ShouldCancel()
    {
        // Arrange
        var uploadContent = FileHelper.GenerateRandomFileContent((Storage is FileSystemStorage) ? 100_0000_000 : 10_0000_000);
        var byteArray = Encoding.ASCII.GetBytes(uploadContent);
        var stream = new MemoryStream(byteArray);
        var cts = new CancellationTokenSource();

        // Act
        var cancellationTask = Task.Run(() =>
        {
            Thread.Sleep(50);
            cts.Cancel();
        });
        var uploadTask = Storage.UploadAsync(stream, cancellationToken: cts.Token);

        await Task.WhenAll(uploadTask, cancellationTask);

        var uploadResult = await uploadTask;

        // Assert
        uploadResult.IsSuccess
            .ShouldBeFalse();
    
     
    }
}
