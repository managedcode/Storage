using FluentAssertions;
using ManagedCode.Storage.Client;
using ManagedCode.Storage.IntegrationTests.Helpers;
using Xunit;

namespace ManagedCode.Storage.IntegrationTests.Tests;

public class AzureControllerTests : BaseControllerTests
{
    public AzureControllerTests(StorageTestApplication testApplication) : base(testApplication)
    {
    }

    [Fact]
    public async Task UploadFile_WhenFileValid_ReturnSuccess()
    {
        // Arrange
        var client = GetHttpClient();
        var storageClient = new StorageClient(client);
        var fileName = "test.txt";
        var contentName = "file";
        var fileToUpload = FileHelper.GenerateLocalFile(fileName, 20000);

        // Act
        var result = await storageClient.UploadFile(fileToUpload.FileStream, "azure/upload", contentName);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
    }
}