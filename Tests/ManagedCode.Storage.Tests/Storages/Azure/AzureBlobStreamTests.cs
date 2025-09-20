using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Shouldly;
using ManagedCode.Storage.Azure;
using ManagedCode.Storage.Core.Models;
using ManagedCode.Storage.Tests.Common;
using ManagedCode.Storage.Tests.Storages.Abstracts;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.Azurite;
using Xunit;

namespace ManagedCode.Storage.Tests.Storages.Azure;

public class AzureBlobStreamTests : StreamTests<AzuriteContainer>
{
    protected override AzuriteContainer Build()
    {
        return new AzuriteBuilder()
            .WithImage(ContainerImages.Azurite)
            .WithCommand("--skipApiVersionCheck")
            .Build();
    }

    protected override ServiceProvider ConfigureServices()
    {
        return AzureConfigurator.ConfigureServices(Container.GetConnectionString());
    }

    [Fact]
    public async Task ReadStreamWithStreamReader_WhenFileExists_ReturnData()
    {
        // Arrange
        var directory = "test-directory";
        var fileSizeInBytes = 10;
        await using var localFile = LocalFile.FromRandomNameWithExtension(".txt");
        FileHelper.GenerateLocalFileWithData(localFile, fileSizeInBytes);
        var storage = (IAzureStorage)Storage;

        UploadOptions options = new() { FileName = localFile.Name, Directory = directory };
        await using var localFileStream = localFile.FileInfo.OpenRead();
        var result = await storage.UploadAsync(localFileStream, options);
        result.IsSuccess.ShouldBeTrue();
        var uploaded = result.Value ?? throw new InvalidOperationException("Upload did not return metadata");

        await using var blobStream = storage.GetBlobStream(uploaded.FullName);

        // Act
        using var streamReader = new StreamReader(blobStream);
        var content = await streamReader.ReadToEndAsync();

        // Assert
        await using var fileStream = localFile.FileInfo.OpenRead();
        using var fileReader = new StreamReader(fileStream);
        var fileContent = await fileReader.ReadToEndAsync();
        content.ShouldNotBeNullOrEmpty();
        fileContent.ShouldNotBeNullOrEmpty();
        content.ShouldBe(fileContent);
    }

    [Fact]
    public async Task ReadStream_WhenFileExists_ReturnData()
    {
        // Arrange
        var directory = "test-directory";
        var fileSizeInBytes = 10;
        await using var localFile = LocalFile.FromRandomNameWithExtension(".txt");
        FileHelper.GenerateLocalFileWithData(localFile, fileSizeInBytes);
        var storage = (IAzureStorage)Storage;

        UploadOptions options = new() { FileName = localFile.Name, Directory = directory };
        await using var fileStream = localFile.FileInfo.OpenRead();
        var result = await storage.UploadAsync(fileStream, options);
        result.IsSuccess.ShouldBeTrue();
        var uploaded = result.Value ?? throw new InvalidOperationException("Upload did not return metadata");

        await using var blobStream = storage.GetBlobStream(uploaded.FullName);

        var chunkSize = (int)blobStream.Length / 2;
        var chunk1 = new byte[chunkSize];
        var chunk2 = new byte[chunkSize];

        // Act
        var bytesReadForChunk1 = await blobStream.ReadAsync(chunk1, 0, chunkSize);
        var bytesReadForChunk2 = await blobStream.ReadAsync(chunk2, 0, chunkSize);

        // Assert
        bytesReadForChunk1.ShouldBe(chunkSize);
        bytesReadForChunk2.ShouldBe(chunkSize);
        chunk1.ShouldNotBeNull();
        chunk1.ShouldNotBeEmpty();
        chunk1.Length.ShouldBe(chunkSize);
        chunk2.ShouldNotBeNull();
        chunk2.ShouldNotBeEmpty();
        chunk2.Length.ShouldBe(chunkSize);
    }

    [Fact]
    public async Task ReadStream_WhenFileDoesNotExists_ReturnNoData()
    {
        // Arrange
        var directory = "test-directory";
        var storage = (IAzureStorage)Storage;
        await storage.CreateContainerAsync();
        var fullFileName = $"{directory}/{Guid.NewGuid()}.txt";

        await using var blobStream = storage.GetBlobStream(fullFileName);
        var chunk = new byte[4];

        // Act
        var bytesRead = await blobStream.ReadAsync(chunk, 0, 4);

        // Assert
        bytesRead.ShouldBe(0);
        chunk.ShouldNotBeNull();
        chunk.ShouldNotBeEmpty();
        chunk.ShouldAllBe(b => b == 0);
    }

    [Fact]
    public async Task WriteStreamWithStreamWriter_SaveData()
    {
        // Arrange
        var directory = "test-directory";
        await using var localFile = LocalFile.FromRandomNameWithExtension(".txt");
        var fileSizeInBytes = 10;
        FileHelper.GenerateLocalFileWithData(localFile, fileSizeInBytes);
        var fullFileName = $"{directory}/{localFile.FileInfo.FullName}";

        var storage = (IAzureStorage)Storage;

        await storage.CreateContainerAsync();

        // Act
        await using (var blobStream = storage.GetBlobStream(fullFileName))
        {
            await using (var localFileStream = localFile.FileStream)
            {
                await localFileStream.CopyToAsync(blobStream);
            }
        }

        // Assert
        var fileResult = await storage.DownloadAsync(fullFileName);
        fileResult.IsSuccess
            .ShouldBeTrue();
        var downloaded = fileResult.Value ?? throw new InvalidOperationException("Download result is null");
        await using var fileStream = downloaded.FileStream;
        using var streamReader = new StreamReader(fileStream);
        var fileContent = await streamReader.ReadLineAsync();
        fileContent.ShouldNotBeNullOrEmpty();
    }

    [Fact]
    public async Task Seek_WhenFileExists_ReturnData()
    {
        // Arrange
        var directory = "test-directory";
        var fileSizeInBytes = 10;
        await using var localFile = LocalFile.FromRandomNameWithExtension(".txt");
        FileHelper.GenerateLocalFileWithData(localFile, fileSizeInBytes);
        var storage = (IAzureStorage)Storage;

        UploadOptions options = new() { FileName = localFile.Name, Directory = directory };
        await using var localFileStream = localFile.FileInfo.OpenRead();
        var result = await storage.UploadAsync(localFileStream, options);
        result.IsSuccess.ShouldBeTrue();
        var uploaded = result.Value ?? throw new InvalidOperationException("Upload did not return metadata");

        await using var blobStream = storage.GetBlobStream(uploaded.FullName);

        // Act
        var seekInPosition = fileSizeInBytes / 2;
        blobStream.Seek(seekInPosition, SeekOrigin.Current);
        var buffer = new byte[seekInPosition];
        var bytesRead = await blobStream.ReadAsync(buffer);

        // Assert
        bytesRead.ShouldBe(seekInPosition);
        await using var fileStream = localFile.FileInfo.OpenRead();
        using var fileReader = new StreamReader(fileStream);
        var fileContent = await fileReader.ReadToEndAsync();
        var content = Encoding.UTF8.GetString(buffer);
        content.ShouldNotBeNullOrEmpty();
        var trimmedFileContent = fileContent.Remove(0, seekInPosition);
        content.ShouldBe(trimmedFileContent);
    }
}
