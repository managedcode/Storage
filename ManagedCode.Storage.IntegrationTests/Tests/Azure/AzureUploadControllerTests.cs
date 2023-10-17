using FluentAssertions;
using ManagedCode.Storage.Client;
using ManagedCode.Storage.IntegrationTests.Constants;
using ManagedCode.Storage.IntegrationTests.Helpers;
using Xunit;

namespace ManagedCode.Storage.IntegrationTests.Tests.Azure;

public class AzureUploadControllerTests : BaseControllerTests
{
    public AzureUploadControllerTests(StorageTestApplication testApplication) : base(testApplication)
    {
    }

    [Fact]
    public async Task UploadFileFromStream_WhenFileValid_ReturnSuccess()
    {
        // Arrange
        var storageClient = GetStorageClient();
        var fileName = "test.txt";
        var contentName = "file";
        var fileToUpload = FileHelper.GenerateLocalFile(fileName, 20000);

        // Act
        var result = await storageClient.UploadFile(fileToUpload.FileStream, ApiEndpoints.Azure.UploadFile, contentName);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
    }

    [Fact]
    public async Task UploadFileFromFileInfo_WhenFileValid_ReturnSuccess()
    {
        // Arrange
        var storageClient = GetStorageClient();
        var fileName = "test.txt";
        var contentName = "file";
        var fileToUpload = FileHelper.GenerateLocalFile(fileName, 20000);

        // Act
        var result = await storageClient.UploadFile(fileToUpload.FileInfo, ApiEndpoints.Azure.UploadFile, contentName);

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
        var fileToUpload = FileHelper.GenerateLocalFile(fileName, 20000);
        var fileAsBytes = await fileToUpload.ReadAllBytesAsync();

        // Act
        var result = await storageClient.UploadFile(fileAsBytes, ApiEndpoints.Azure.UploadFile, contentName);

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
        var fileToUpload = FileHelper.GenerateLocalFile(fileName, 20000);
        var fileAsBytes = await fileToUpload.ReadAllBytesAsync();
        var fileAsString64 = Convert.ToBase64String(fileAsBytes);

        // Act
        var result = await storageClient.UploadFile(fileAsString64, ApiEndpoints.Azure.UploadFile, contentName);
        
        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
    }
}