using FluentAssertions;
using ManagedCode.Storage.Client;
using ManagedCode.Storage.IntegrationTests.Constants;
using ManagedCode.Storage.IntegrationTests.Helpers;
using Xunit;

namespace ManagedCode.Storage.IntegrationTests.Tests.Azure;

public class AzureUploadControllerTests : BaseUploadControllerTests
{
    public AzureUploadControllerTests(StorageTestApplication testApplication) : base(testApplication)
    {
    }

    [Fact]
    public async Task UploadFileFromStream_WhenFileValid_ReturnSuccess()
    {
        // Arrange
        var client = GetHttpClient();
        var storageClient = new StorageClient(client);
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
        var client = GetHttpClient();
        var storageClient = new StorageClient(client);
        var fileName = "test.txt";
        var contentName = "file";
        var fileToUpload = FileHelper.GenerateLocalFile(fileName, 20000);

        // Act
        var result = await storageClient.UploadFile(fileToUpload.FileInfo, ApiEndpoints.Azure.UploadFile, contentName);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
    }
}