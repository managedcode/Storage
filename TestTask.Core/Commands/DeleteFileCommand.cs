using Domain;
using ManagedCode.Communication;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RestSharp;
using TestTask.Infrastructure;
using TestTask.Infrastructure.Abstractions;
using TestTask.Infrastructure.Configuration;
using TestTask.Infrastructure.Extensions;

namespace TestTask.Core.Commands;

public record DeleteFileCommand(string FileName, ProviderType ProviderType) : IRequest<Result<bool>>;

public class DeleteFileCommandHandler(
    ILogger<DeleteFileCommandHandler> logger,
    IRestClientFabric restClientFabric,
    IOptions<RoutesConfiguration> routesConfig)
    : IRequestHandler<DeleteFileCommand, Result<bool>>
{
    private readonly ILogger _logger = logger;

    public async Task<Result<bool>> Handle(DeleteFileCommand request, CancellationToken cancellationToken)
    {
        var routes = routesConfig.Value;
        var restClient = restClientFabric.GetRestClient(request.ProviderType);
        var restRequest = new RestRequest($"{routes.DeleteRoute}/{request.FileName}", Method.Delete);
        var response = await restClient.HandleRestClientResponse<bool>(restRequest, _logger, cancellationToken);
        return response;
    }
}