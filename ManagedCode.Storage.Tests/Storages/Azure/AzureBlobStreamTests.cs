using System;
using System.IO;
using System.Threading.Tasks;
using FluentAssertions;
using ManagedCode.Storage.Azure;
using ManagedCode.Storage.Core.Models;
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
    public async Task ReadStream_WhenFileExists_ReturnData()
    {
        // Arrange
        var directory = "test-directory";
        var fileName = Guid.NewGuid().ToString();
        await using var localFile = LocalFile.FromRandomNameWithExtension(".txt");
        FileHelper.GenerateLocalFileWithData(localFile, 10);
        var storage = (IAzureStorage) Storage;
        
        UploadOptions options = new() { FileName = localFile.Name, Directory = directory };
        var result = await storage.UploadAsync(localFile.FileInfo.OpenRead(), options);

        await using var blobStream = storage.GetBlobStream(result.Value.FullName);

        var chunkSize = localFile.FileInfo.Length;
        byte[] fileChunk1 = new byte[chunkSize];
        byte[] fileChunk2 = new byte[chunkSize];
        
        // Act
        using var streamReader = new StreamReader(blobStream);
        var content = await streamReader.ReadToEndAsync();
        
        // Assert
        content.Should().NotBeNullOrEmpty();
        using var fileReader = new StreamReader(localFile.FileStream);
        var fileContent = await fileReader.ReadToEndAsync();
        fileContent.Should().NotBeNullOrEmpty();
        content.Should().Be(fileContent);
    }
}