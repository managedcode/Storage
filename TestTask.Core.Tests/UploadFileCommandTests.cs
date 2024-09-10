using Domain;
using ManagedCode.Communication;
using ManagedCode.Storage.Core.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using RestSharp;
using TestTask.Core.Commands;
using TestTask.Infrastructure.Abstractions;
using TestTask.Infrastructure.Configuration;
using TestTask.Infrastructure.Extensions;

namespace TestTask.Core.Tests;

public class UploadFileCommandTests
{
    private readonly Mock<ILogger<UploadFileCommandHandler>> _loggerMock;
    private readonly Mock<IRestClientFabric> _restClientFabricMock;
    private readonly Mock<IOptions<RoutesConfiguration>> _routesConfigMock;
    private readonly Mock<RestClient> _restClientMock;
    private readonly Mock<IChunkedFileTransferUtility> _chunkedFileTransferUtilityMock;

    public UploadFileCommandTests()
    {
        _loggerMock = new Mock<ILogger<UploadFileCommandHandler>>();
        _restClientFabricMock = new Mock<IRestClientFabric>();
        _routesConfigMock = new Mock<IOptions<RoutesConfiguration>>();
        _restClientMock = new Mock<RestClient>();
        _chunkedFileTransferUtilityMock = new Mock<IChunkedFileTransferUtility>();

        _routesConfigMock.Setup(x => x.Value).Returns(new RoutesConfiguration
        {
            UploadRoute = "/upload",
            DownloadRoute = "null",
            DeleteRoute = "null"
        });
    }

    [Fact]
    public async Task Handle_ShouldUploadFile_WhenSizeIsLessThan1GB()
    {
        // Arrange
        var fileName = "testfile.txt";
        var provider = ProviderType.AWS;
        var fileStream = new MemoryStream(new byte[100]);
        var command = new UploadFileCommand(fileStream, fileName, provider);

        _restClientFabricMock.Setup(x => x.GetRestClient(provider)).Returns(_restClientMock.Object);
        _restClientMock.Setup(x => x.HandleRestClientResponse<BlobMetadata>(It.IsAny<RestRequest>(), It.IsAny<ILogger>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<BlobMetadata>.Succeed(new BlobMetadata { Name = fileName, Length = fileStream.Length }));

        var handler = new UploadFileCommandHandler(_loggerMock.Object, _restClientFabricMock.Object, _routesConfigMock.Object, _chunkedFileTransferUtilityMock.Object);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(fileName, result.Value.Name);
    }

    [Fact]
    public async Task Handle_ShouldUseChunkedUpload_WhenFileSizeExceeds1GB()
    {
        // Arrange
        var fileName = "largefile.txt";
        var provider = ProviderType.AWS;

        // Instead of allocating 2 GB memory, mock the length of the stream
        var mockStream = new Mock<Stream>();
        mockStream.Setup(s => s.Length).Returns(unchecked(2 * 1024 * 1024 * 1024)); // 2 GB

        var command = new UploadFileCommand(mockStream.Object, fileName, provider);

        _restClientFabricMock.Setup(x => x.GetRestClient(provider)).Returns(_restClientMock.Object);
        _chunkedFileTransferUtilityMock.Setup(x => x.UploadFileInChunksAsync(_restClientMock.Object, It.IsAny<string>(), fileName, mockStream.Object, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Succeed());

        var handler = new UploadFileCommandHandler(_loggerMock.Object, _restClientFabricMock.Object, _routesConfigMock.Object, _chunkedFileTransferUtilityMock.Object);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(fileName, result.Value.Name);

        // Verify that chunked upload was used
        _chunkedFileTransferUtilityMock.Verify(x => x.UploadFileInChunksAsync(_restClientMock.Object, It.IsAny<string>(), fileName, mockStream.Object, It.IsAny<CancellationToken>()), Times.Once);
    }
}