using Domain;
using ManagedCode.Communication;
using ManagedCode.Storage.Core.Models;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RestSharp;
using TestTask.Infrastructure.Abstractions;
using TestTask.Infrastructure.Configuration;
using TestTask.Infrastructure.Extensions;

namespace TestTask.Core.Commands;

public record UploadFileCommand(Stream FileStream, string FileName, ProviderType ProviderType) : IRequest<Result<BlobMetadata>>;

public class UploadFileCommandHandler(
    ILogger<UploadFileCommandHandler> logger,
    IRestClientFabric restClientFabric,
    IOptions<RoutesConfiguration> routesConfig,
    IChunkedFileTransferUtility chunkedFileTransferUtility)
    : IRequestHandler<UploadFileCommand, Result<BlobMetadata>>
{
    private const long LargeFileSizeThreshold = 1 * 1024 * 1024 * 1024; // 1 GB

    private readonly ILogger _logger = logger;

    public async Task<Result<BlobMetadata>> Handle(UploadFileCommand request, CancellationToken cancellationToken)
    {
        var routes = routesConfig.Value;
        var restClient = restClientFabric.GetRestClient(request.ProviderType);
        var fileLength = request.FileStream.Length;

        if (fileLength > LargeFileSizeThreshold)
        {
            var uploadResult = await chunkedFileTransferUtility.UploadFileInChunksAsync(restClient, routes.UploadRoute, request.FileName, request.FileStream, cancellationToken);
            if (!uploadResult.IsSuccess)
                return uploadResult;
            var metadata = new BlobMetadata { Name = request.FileName, Length = fileLength };
            return Result<BlobMetadata>.Succeed(metadata);
        }
        var fileBytes = new byte[fileLength];
        await request.FileStream.ReadAsync(fileBytes, cancellationToken);

        var restRequest = new RestRequest(routes.UploadRoute, Method.Post);
        restRequest.AddFile(request.FileName, fileBytes, request.FileName);

        return await restClient.HandleRestClientResponse<BlobMetadata>(restRequest, _logger, cancellationToken);
    }
}
