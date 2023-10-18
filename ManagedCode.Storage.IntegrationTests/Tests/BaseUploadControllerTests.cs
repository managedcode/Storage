using System.Net;
using FluentAssertions;
using ManagedCode.Storage.Core.Models;
using ManagedCode.Storage.IntegrationTests.Constants;
using ManagedCode.Storage.IntegrationTests.Helpers;
using Xunit;

namespace ManagedCode.Storage.IntegrationTests.Tests;

public abstract class BaseUploadControllerTests : BaseControllerTests
{
    private readonly string _uploadEndpoint;
    private readonly string _uploadChunksEndpoint;
    
    protected BaseUploadControllerTests(StorageTestApplication testApplication, string apiEndpoint) : base(testApplication, apiEndpoint)
    {
        _uploadEndpoint = string.Format(ApiEndpoints.Base.UploadFile, ApiEndpoint);
        _uploadChunksEndpoint = string.Format(ApiEndpoints.Base.UploadFileChunks, ApiEndpoint);
    }
    
    [Fact]
    public async Task UploadFileFromStream_WhenFileValid_ReturnSuccess()
    {
        // Arrange
        var storageClient = GetStorageClient();
        var contentName = "file";

        await using var localFile = LocalFile.FromRandomNameWithExtension(".txt");
        FileHelper.GenerateLocalFile(localFile, 1);

        // Act
        var result = await storageClient.UploadFile(localFile.FileStream, _uploadEndpoint, contentName);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
    }
    
    [Fact]
    public async Task UploadFileFromStream_WhenFileSizeIsForbidden_ReturnFail()
    {
        // Arrange
        var storageClient = GetStorageClient();
        var contentName = "file";

        await using var localFile = LocalFile.FromRandomNameWithExtension(".txt");
        FileHelper.GenerateLocalFile(localFile, 200);

        // Act
        var result = await storageClient.UploadFile(localFile.FileStream, _uploadEndpoint, contentName);

        // Assert
        result.IsFailed.Should().BeTrue();
        result.GetError().Value.ErrorCode.Should().Be(HttpStatusCode.BadRequest.ToString());
    }

    [Fact]
    public async Task UploadFileFromFileInfo_WhenFileValid_ReturnSuccess()
    {
        // Arrange
        var storageClient = GetStorageClient();
        var fileName = "test.txt";
        var contentName = "file";
        
        await using var localFile = LocalFile.FromRandomNameWithExtension(".txt");
        FileHelper.GenerateLocalFile(localFile, 1);

        // Act
        var result = await storageClient.UploadFile(localFile.FileInfo, _uploadEndpoint, contentName);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
    }

    [Fact]
     public async Task UploadFileFromBytes_WhenFileValid_ReturnSuccess()
     {
         // Arrange
         var storageClient = GetStorageClient();
         var fileName = "test.txt";
         var contentName = "file";
         await using var localFile = LocalFile.FromRandomNameWithExtension(".txt");
         FileHelper.GenerateLocalFile(localFile, 1);
         
         var fileAsBytes = await localFile.ReadAllBytesAsync();
    
         // Act
         var result = await storageClient.UploadFile(fileAsBytes, _uploadEndpoint, contentName);
    
         // Assert
         result.IsSuccess.Should().BeTrue();
         result.Value.Should().NotBeNull();
     }
    
     [Fact]
     public async Task UploadFileFromBase64String_WhenFileValid_ReturnSuccess()
     {
         // Arrange
         var storageClient = GetStorageClient();
         var fileName = "test.txt";
         var contentName = "file";
         
         await using var localFile = LocalFile.FromRandomNameWithExtension(".txt");
         FileHelper.GenerateLocalFile(localFile, 1);
         
         var fileAsBytes = await localFile.ReadAllBytesAsync();
         var fileAsString64 = Convert.ToBase64String(fileAsBytes);
    
         // Act
         var result = await storageClient.UploadFile(fileAsString64, _uploadEndpoint, contentName);
         
         // Assert
         result.IsSuccess.Should().BeTrue();
         result.Value.Should().NotBeNull();
     }
     
     [Fact]
     public async Task UploadFileInChunks_WhenFileValid_ReturnSuccess()
     {
         // Arrange
         var storageClient = GetStorageClient();
         var fileName = "test.txt";
         var contentName = "file";
         
         await using var localFile = LocalFile.FromRandomNameWithExtension(".txt");
         FileHelper.GenerateLocalFile(localFile, 20);
    
         // Act
         var result = await storageClient.UploadFileInChunks(localFile.FileStream, _uploadChunksEndpoint, 100000000);
         
         // Assert
         result.IsSuccess.Should().BeTrue();
         //result.Value.Should().NotBeNull();
     }
}