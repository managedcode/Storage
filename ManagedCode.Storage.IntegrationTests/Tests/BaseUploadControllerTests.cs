using System.Net;
using FluentAssertions;
using ManagedCode.Storage.Core.Helpers;
using ManagedCode.Storage.Core.Models;
using ManagedCode.Storage.IntegrationTests.Constants;
using Xunit;

namespace ManagedCode.Storage.IntegrationTests.Tests;

public abstract class BaseUploadControllerTests : BaseControllerTests
{
    private readonly string _uploadEndpoint;
    private readonly string _uploadChunksStreamEndpoint;
    private readonly string _uploadChunksMergeEndpoint;

    protected BaseUploadControllerTests(StorageTestApplication testApplication, string apiEndpoint) : base(testApplication, apiEndpoint)
    {
        _uploadEndpoint = string.Format(ApiEndpoints.Base.UploadFile, ApiEndpoint);
        _uploadChunksStreamEndpoint = string.Format(ApiEndpoints.Base.UploadFileChunksUsingStream, ApiEndpoint);
        _uploadChunksMergeEndpoint = string.Format(ApiEndpoints.Base.UploadFileChunksUsingMerge, ApiEndpoint);
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
     public async Task UploadFileInChunksUsingStream_WhenFileValid_ReturnSuccess()
     {
         // Arrange
         var storageClient = GetStorageClient();
         var fileName = "test.txt";
         var contentName = "file";
         
         await using var localFile = LocalFile.FromRandomNameWithExtension(".txt");
         FileHelper.GenerateLocalFile(localFile, 200);
    
         // Act
         var result = await storageClient.UploadLargeFileUsingStream(localFile.FileStream, _uploadChunksStreamEndpoint + "/create", 
             _uploadChunksStreamEndpoint + "/upload", _uploadChunksStreamEndpoint + "/complete", null, new CancellationToken());
         
         // Assert
         result.IsSuccess.Should().BeTrue();
         //result.Value.Should().NotBeNull();
     }
     
     [Fact]
     public async Task UploadFileInChunksUsingMerge_WhenFileValid_ReturnSuccess()
     {
         // Arrange
         var storageClient = GetStorageClient();
         var fileName = "test.txt";
         var contentName = "file";
         
         await using var localFile = LocalFile.FromRandomNameWithExtension(".txt");
         FileHelper.GenerateLocalFile(localFile, 200);
    
         //Act
         var result = await storageClient.UploadLargeFileUsingMerge(localFile.FileStream,
             _uploadChunksMergeEndpoint + "/upload", 
             _uploadChunksMergeEndpoint + "/complete", 
             null,
             new CancellationToken());
         
         // Assert
         result.IsSuccess.Should().BeTrue();
         //result.Value.Should().NotBeNull();
     }
     
     //Task<Result> UploadLargeFileUsingMerge(Stream file, string uploadApiUrl, string mergeApiUrl, Action<double>? onProgressChanged, CancellationToken cancellationToken);

}