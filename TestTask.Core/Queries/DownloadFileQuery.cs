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

namespace TestTask.Core.Queries;

public record DownloadFileQuery(string FileName, ProviderType ProviderType) : IRequest<Result<LocalFile>>;

public class DownloadFileQueryHandler(
    ILogger<DownloadFileQuery> logger,
    IRestClientFabric restClientFabric,
    IOptions<RoutesConfiguration> routesConfig,
    IChunkedFileTransferUtility chunkedFileTransferUtility)
    : IRequestHandler<DownloadFileQuery, Result<LocalFile>>
{
    private const long LargeFileSizeThreshold = 1 * 1024 * 1024 * 1024; // 1 GB

    private readonly ILogger _logger = logger;

    public async Task<Result<LocalFile>> Handle(DownloadFileQuery request, CancellationToken cancellationToken)
    {
        var routes = routesConfig.Value;
        var restClient = restClientFabric.GetRestClient(request.ProviderType);
        var metadataRequest = new RestRequest($"{routes.DownloadRoute}/{request.FileName}");
        var metadataResponse = await restClient.HandleRestClientResponse<BlobMetadata>(metadataRequest, _logger, cancellationToken);

        if (!metadataResponse.IsSuccess || metadataResponse.Value == null)
        {
            return Result<LocalFile>.Fail("Failed to retrieve file metadata");
        }

        var fileLength = metadataResponse.Value.Length;

        if (fileLength > LargeFileSizeThreshold)
        {
            var downloadResult = await chunkedFileTransferUtility.DownloadFileInChunksAsync(restClient, routes.DownloadRoute, request.FileName, fileLength, cancellationToken);

            if (downloadResult.IsSuccess)
            {
                var localFile = await LocalFile.FromStreamAsync(downloadResult.Value, request.FileName);
                return Result<LocalFile>.Succeed(localFile);
            }

            return Result<LocalFile>.Fail(downloadResult);
        }
        var restRequest = new RestRequest($"{routes.DownloadRoute}/{request.FileName}");
        var response = await restClient.ExecuteAsync(restRequest, cancellationToken);

        if (response.IsSuccessful)
        {
            var stream = new MemoryStream(response.RawBytes);
            var localFile = await LocalFile.FromStreamAsync(stream, request.FileName);
            return Result<LocalFile>.Succeed(localFile);
        }

        _logger.LogError("Failed to download file: {0}", response.StatusCode);
        return Result<LocalFile>.Fail(response.StatusCode);
    }
}