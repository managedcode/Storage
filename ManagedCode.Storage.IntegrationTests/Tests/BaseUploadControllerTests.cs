using Xunit;

namespace ManagedCode.Storage.IntegrationTests.Tests;

[Collection(nameof(StorageTestApplication))]
public abstract class BaseUploadControllerTests
{
    protected readonly StorageTestApplication TestApplication;

    protected BaseUploadControllerTests(StorageTestApplication testApplication)
    {
        TestApplication = testApplication;
    }

    protected HttpClient GetHttpClient()
    {
        return TestApplication.CreateClient();
    }
}