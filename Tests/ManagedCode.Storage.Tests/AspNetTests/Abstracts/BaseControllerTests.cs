using System.Net.Http;
using ManagedCode.Storage.Client;
using Xunit;

namespace ManagedCode.Storage.Tests.Common.Tests;

[Collection(nameof(StorageTestApplication))]
public abstract class BaseControllerTests
{
    protected readonly string ApiEndpoint;
    protected readonly StorageTestApplication TestApplication;

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
}