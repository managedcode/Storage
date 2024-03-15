using ManagedCode.Storage.IntegrationTests.Constants;

namespace ManagedCode.Storage.IntegrationTests.Tests.FileSystem;

public class FileSystemDownloadControllerTests(StorageTestApplication testApplication)
    : BaseUploadControllerTests(testApplication, ApiEndpoints.FileSystem)
{
    // USING API CLIENT TO TEST API CALLS
}