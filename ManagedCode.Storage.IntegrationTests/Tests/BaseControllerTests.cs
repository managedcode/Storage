using Xunit;

namespace ManagedCode.Storage.IntegrationTests.Tests;

[Collection(nameof(StorageTestApplication))]
public abstract class BaseControllerTests
{
    protected readonly StorageTestApplication TestApplication;

    protected BaseControllerTests(StorageTestApplication testApplication)
    {
        TestApplication = testApplication;
    }

    protected HttpClient GetHttpClient()
    {
        return TestApplication.CreateClient();
    }
}