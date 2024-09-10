using Domain;
using RestSharp;

namespace TestTask.Infrastructure.Abstractions;

public interface IRestClientFabric
{
    RestClient GetRestClient(ProviderType providerType);
}