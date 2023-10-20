using ManagedCode.Storage.IntegrationTests.Constants;

namespace ManagedCode.Storage.IntegrationTests.Tests.Azure;

public class AzureUploadControllerTests : BaseUploadControllerTests
{
    public AzureUploadControllerTests(StorageTestApplication testApplication) : base(testApplication, ApiEndpoints.Azure)
    {
    }
}