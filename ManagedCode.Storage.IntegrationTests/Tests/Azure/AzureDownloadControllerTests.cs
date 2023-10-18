using ManagedCode.Storage.IntegrationTests.Constants;

namespace ManagedCode.Storage.IntegrationTests.Tests.Azure;

public class AzureDownloadControllerTests : BaseDownloadControllerTests
{
    public AzureDownloadControllerTests(StorageTestApplication testApplication) : base(testApplication, ApiEndpoints.Azure)
    {
    }
}