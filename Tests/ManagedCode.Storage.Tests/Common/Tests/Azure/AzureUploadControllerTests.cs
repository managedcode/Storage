using ManagedCode.Storage.Tests.Common.Constants;

namespace ManagedCode.Storage.Tests.Common.Tests.Azure;

public class AzureUploadControllerTests : BaseUploadControllerTests
{
    public AzureUploadControllerTests(StorageTestApplication testApplication) : base(testApplication, ApiEndpoints.Azure)
    {
    }
}