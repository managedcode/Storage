using Domain;
using ManagedCode.Communication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using RestSharp;
using TestTask.Core.Commands;
using TestTask.Infrastructure.Abstractions;
using TestTask.Infrastructure.Configuration;
using TestTask.Infrastructure.Extensions;

namespace TestTask.Core.Tests;

public class DeleteFileCommandHandlerTests
{
    private readonly Mock<ILogger<DeleteFileCommandHandler>> _loggerMock;
    private readonly Mock<IRestClientFabric> _restClientFabricMock;
    private readonly Mock<IOptions<RoutesConfiguration>> _routesConfigMock;
    private readonly Mock<RestClient> _restClientMock;
    
    public DeleteFileCommandHandlerTests()
    {
        _loggerMock = new Mock<ILogger<DeleteFileCommandHandler>>();
        _restClientFabricMock = new Mock<IRestClientFabric>();
        _routesConfigMock = new Mock<IOptions<RoutesConfiguration>>();
        _restClientMock = new Mock<RestClient>();

        _routesConfigMock.Setup(x => x.Value).Returns(new RoutesConfiguration
        {
            DeleteRoute = "/delete",
            UploadRoute = "null",
            DownloadRoute = "null"
        });
    }

    [Fact]
    public async Task Handle_ShouldReturnSuccess_WhenDeleteSucceeds()
    {
        // Arrange
        var fileName = "testfile.txt";
        var provider = ProviderType.AWS;
        var command = new DeleteFileCommand(fileName, provider);

        _restClientFabricMock.Setup(x => x.GetRestClient(provider)).Returns(_restClientMock.Object);
        _restClientMock.Setup(x => x.HandleRestClientResponse<bool>(It.IsAny<RestRequest>(), It.IsAny<ILogger>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<bool>.Succeed(true));

        var handler = new DeleteFileCommandHandler(_loggerMock.Object, _restClientFabricMock.Object, _routesConfigMock.Object);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task Handle_ShouldReturnFail_WhenDeleteFails()
    {
        // Arrange
        var fileName = "testfile.txt";
        var provider = ProviderType.AWS;
        var command = new DeleteFileCommand(fileName, provider);

        _restClientFabricMock.Setup(x => x.GetRestClient(provider)).Returns(_restClientMock.Object);
        _restClientMock.Setup(x => x.HandleRestClientResponse<bool>(It.IsAny<RestRequest>(), It.IsAny<ILogger>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<bool>.Fail("Delete failed"));

        var handler = new DeleteFileCommandHandler(_loggerMock.Object, _restClientFabricMock.Object, _routesConfigMock.Object);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("Delete failed", result.GetError().GetValueOrDefault().Message);
    }
}