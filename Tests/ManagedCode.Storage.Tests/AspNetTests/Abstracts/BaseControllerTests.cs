using System.Net.Http;
using ManagedCode.Storage.Client;
using ManagedCode.Storage.Tests.Common;
using Xunit;

namespace ManagedCode.Storage.Tests.AspNetTests.Abstracts;

[Collection(nameof(StorageTestApplication))]
public abstract class BaseControllerTests(StorageTestApplication testApplication, string apiEndpoint)
{
    protected readonly string ApiEndpoint = apiEndpoint;
    protected readonly StorageTestApplication TestApplication = testApplication;

    protected HttpClient GetHttpClient()
    {
        return TestApplication.CreateClient();
    }

    protected IStorageClient GetStorageClient()
    {
        return new StorageClient(TestApplication.CreateClient());
    }
}