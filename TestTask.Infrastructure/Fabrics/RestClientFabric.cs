using Domain;
using RestSharp;
using TestTask.Infrastructure.Abstractions;

namespace TestTask.Infrastructure.Fabrics
{
    public class RestClientFabric: IRestClientFabric
    {
        public RestClient GetRestClient(ProviderType providerType)
        {
            return providerType switch
            {
                ProviderType.AWS => ConfigureAwsClient(),
                ProviderType.GCP => ConfigureGcpClient(),
                ProviderType.Azure => ConfigureAzureClient(),
                ProviderType.FileStorage => ConfigureFileStorageClient(),
                _ => throw new ArgumentOutOfRangeException(nameof(providerType), "Unknown provider type")
            };
        }

        private static RestClient ConfigureAwsClient()
        {
            var client = new RestClient("https://aws-api-url.com");
            client.AddDefaultHeader("Authorization", "Bearer AWSAuthToken");
            return client;
        }

        private static RestClient ConfigureGcpClient()
        {
            var client = new RestClient("https://gcp-api-url.com");
            client.AddDefaultHeader("Authorization", "Bearer GCPAuthToken");
            return client;
        }

        private static RestClient ConfigureAzureClient()
        {
            var client = new RestClient("https://azure-api-url.com");
            client.AddDefaultHeader("Authorization", "Bearer AzureAuthToken");
            return client;
        }

        private static RestClient ConfigureFileStorageClient()
        {
            var client = new RestClient("https://filestorage-api-url.com");
            client.AddDefaultHeader("Authorization", "Bearer FileStorageToken");
            return client;
        }
    }
}
