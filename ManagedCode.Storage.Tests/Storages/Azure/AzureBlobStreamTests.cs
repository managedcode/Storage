using System;
using System.IO;
using System.Threading.Tasks;
using FluentAssertions;
using ManagedCode.Storage.Azure;
using ManagedCode.Storage.Core.Models;
using ManagedCode.Storage.Server;
using ManagedCode.Storage.Tests.Common;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.Azurite;
using Xunit;

namespace ManagedCode.Storage.Tests.Storages.Azure;

public class AzureBlobStreamTests : StreamTests<AzuriteContainer>
{
    protected override AzuriteContainer Build()
    {
        return new AzuriteBuilder()
            .WithImage("mcr.microsoft.com/azure-storage/azurite:3.26.0")
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
        await using var localFile = LocalFile.FromRandomNameWithExtension(".txt");
        FileHelper.GenerateLocalFileWithData(localFile, 10);
        var storage = (IAzureStorage) Storage;
        
        UploadOptions options = new() { FileName = localFile.Name, Directory = directory };
        await using var localFileStream = localFile.FileInfo.OpenRead();
        var result = await storage.UploadAsync(localFileStream, options);

        await using var blobStream = storage.GetBlobStream(result.Value.FullName);
        
        // Act
        using var streamReader = new StreamReader(blobStream);
        var content = await streamReader.ReadToEndAsync();
        
        // Assert
        await using var fileStream = localFile.FileInfo.OpenRead();
        using var fileReader = new StreamReader(fileStream);
        var fileContent = await fileReader.ReadToEndAsync();
        content.Should().NotBeNullOrEmpty();
        fileContent.Should().NotBeNullOrEmpty();
        content.Should().Be(fileContent);
    }

    [Fact]
    public async Task ReadStream_WhenFileExists_ReturnData()
    {
        // Arrange
        var directory = "test-directory";
        await using var localFile = LocalFile.FromRandomNameWithExtension(".txt");
        FileHelper.GenerateLocalFileWithData(localFile, 10);
        var storage = (IAzureStorage) Storage;
        
        UploadOptions options = new() { FileName = localFile.Name, Directory = directory };
        await using var fileStream = localFile.FileInfo.OpenRead();
        var result = await storage.UploadAsync(fileStream, options);
    
        await using var blobStream = storage.GetBlobStream(result.Value.FullName);

        var chunkSize = (int) blobStream.Length / 2;
        var chunk1 = new byte[chunkSize];
        var chunk2 = new byte[chunkSize];

        // Act
        var bytesReadForChunk1 = await blobStream.ReadAsync(chunk1, 0, chunkSize);
        var bytesReadForChunk2 = await blobStream.ReadAsync(chunk2, 0, chunkSize);

        // Assert
        bytesReadForChunk1.Should().Be(chunkSize);
        bytesReadForChunk2.Should().Be(chunkSize);
        chunk1.Should().NotBeNullOrEmpty().And.HaveCount(chunkSize);
        chunk2.Should().NotBeNullOrEmpty().And.HaveCount(chunkSize);
    }

    [Fact]
    public async Task ReadStream_WhenFileDoesNotExists_ReturnNoData()
    {
        // Arrange
        var directory = "test-directory";
        var storage = (IAzureStorage) Storage;
        await storage.CreateContainerAsync();
        var fullFileName = $"{directory}/{Guid.NewGuid()}.txt";
        
        await using var blobStream = storage.GetBlobStream(fullFileName);
        var chunk = new byte[4];
        
        // Act
        var bytesRead = await blobStream.ReadAsync(chunk, 0, 4);
        
        // Assert
        bytesRead.Should().Be(0);
        chunk.Should().NotBeNullOrEmpty();
        chunk.Should().AllBeEquivalentTo(0);
    }

    [Fact]
    public async Task WriteStreamWithStreamWriter_WhenFileExists_SaveData()
    {
        // Arrange
        var directory = "test-directory";
        await using var localFile = LocalFile.FromRandomNameWithExtension(".txt");
        FileHelper.GenerateLocalFileWithData(localFile, 512);
        var fullFileName = $"{directory}/{localFile.FileInfo.FullName}";

        var storage = (IAzureStorage) Storage;
        
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
        fileResult.IsSuccess.Should().BeTrue();
        fileResult.Value.Should().NotBeNull();
        await using var fileStream = fileResult.Value.FileStream;
        using var streamReader = new StreamReader(fileStream);
        var fileContent = await streamReader.ReadLineAsync();
        fileContent.Should().NotBeNullOrEmpty();
    }
}