using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using ManagedCode.Storage.CloudKit;
using ManagedCode.Storage.CloudKit.Options;
using Shouldly;
using Xunit;

namespace ManagedCode.Storage.Tests.Storages.CloudKit;

public class CloudKitStorageTests
{
    [Fact]
    public async Task CloudKitStorage_RoundTrip_WithHttpHandler()
    {
        var handler = new FakeCloudKitHttpHandler();
        var httpClient = new HttpClient(handler);

        var storage = new CloudKitStorage(new CloudKitStorageOptions
        {
            ContainerId = "iCloud.com.example.app",
            Environment = CloudKitEnvironment.Development,
            Database = CloudKitDatabase.Public,
            ApiToken = "test-token",
            RootPath = "app-data",
            HttpClient = httpClient
        });

        var upload = await storage.UploadAsync("storage payload", options =>
        {
            options.Directory = "dir";
            options.FileName = "file.txt";
        });

        upload.IsSuccess.ShouldBeTrue();
        upload.Value.FullName.ShouldBe("dir/file.txt");
        upload.Value.Container.ShouldBe("iCloud.com.example.app");

        var download = await storage.DownloadAsync("dir/file.txt");
        download.IsSuccess.ShouldBeTrue();
        using (var reader = new StreamReader(download.Value.FileStream, Encoding.UTF8))
        {
            (await reader.ReadToEndAsync()).ShouldBe("storage payload");
        }

        var existsBeforeDelete = await storage.ExistsAsync("dir/file.txt");
        existsBeforeDelete.IsSuccess.ShouldBeTrue();
        existsBeforeDelete.Value.ShouldBeTrue();

        var deleteDir = await storage.DeleteDirectoryAsync("dir");
        deleteDir.IsSuccess.ShouldBeTrue();

        var existsAfterDelete = await storage.ExistsAsync("dir/file.txt");
        existsAfterDelete.IsSuccess.ShouldBeTrue();
        existsAfterDelete.Value.ShouldBeFalse();
    }
}
