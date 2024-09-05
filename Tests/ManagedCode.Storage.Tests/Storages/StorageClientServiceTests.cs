using ManagedCode.Storage.BlobClient.Services;
using Microsoft.Extensions.Configuration;
using Moq;
using System.Net.Http;
using Xunit;

public class StorageClientServiceTests
{
    [Fact]
    public void StorageClientService_ShouldBeCreatedSuccessfully()
    {
        // Arrange: Mock the dependencies.
        var mockHttpClient = new Mock<HttpClient>();
        var mockConfiguration = new Mock<IConfiguration>();

        // Act: Create an instance of StorageClientService.
        var storageClientService = new StorageClientService(mockHttpClient.Object, mockConfiguration.Object);

        // Assert: Ensure the service is created successfully.
        Assert.NotNull(storageClientService);
    }
}
