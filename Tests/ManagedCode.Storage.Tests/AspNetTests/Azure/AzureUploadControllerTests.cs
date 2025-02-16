using ManagedCode.Storage.Tests.AspNetTests.Abstracts;
using ManagedCode.Storage.Tests.Common;
using ManagedCode.Storage.Tests.Constants;

namespace ManagedCode.Storage.Tests.AspNetTests.Azure;

public class AzureUploadControllerTests : BaseUploadControllerTests
{
    public AzureUploadControllerTests(StorageTestApplication testApplication) : base(testApplication, ApiEndpoints.Azure)
    {
    }
}