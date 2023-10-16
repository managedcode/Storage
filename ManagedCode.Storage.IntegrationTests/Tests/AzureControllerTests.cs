using FluentAssertions;
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

        // Act
        var result = await client.PostAsync("azure/upload", null);

        // Assert
        result.IsSuccessStatusCode.Should().BeTrue();
    }
}