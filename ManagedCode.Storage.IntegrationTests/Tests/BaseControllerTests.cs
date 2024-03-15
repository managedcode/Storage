using BlobStorageAccessClient;
using ManagedCode.Storage.Client;
using Xunit;

namespace ManagedCode.Storage.IntegrationTests.Tests;

[Collection(nameof(StorageTestApplication))]
public abstract class BaseControllerTests
{
    protected readonly StorageTestApplication TestApplication;
    protected readonly string ApiEndpoint;

    protected BaseControllerTests(StorageTestApplication testApplication, string apiEndpoint)
    {
        TestApplication = testApplication;
        ApiEndpoint = apiEndpoint;
    }

    protected HttpClient GetHttpClient()
    {
        return TestApplication.CreateClient();
    }

    protected IStorageClient GetStorageClient()
    {
        return new StorageClient(TestApplication.CreateClient());
    }

    protected IApiClient GetApiClient()
    {
        return new ApiClient(TestApplication.CreateClient());
    }
    
}