using System;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using ManagedCode.Storage.CloudKit;
using ManagedCode.Storage.CloudKit.Options;
using ManagedCode.Storage.Core.Models;
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
        }, TestContext.Current.CancellationToken);

        upload.IsSuccess.ShouldBeTrue();
        upload.Value.FullName.ShouldBe("dir/file.txt");
        upload.Value.Container.ShouldBe("iCloud.com.example.app");

        var download = await storage.DownloadAsync("dir/file.txt", TestContext.Current.CancellationToken);
        download.IsSuccess.ShouldBeTrue();
        using (var reader = new StreamReader(download.Value.FileStream, Encoding.UTF8))
        {
            (await reader.ReadToEndAsync(TestContext.Current.CancellationToken)).ShouldBe("storage payload");
        }

        var token = TestContext.Current.CancellationToken;
        var destination = Path.Combine(Environment.CurrentDirectory, $"cloudkit-{Guid.NewGuid():N}");
        try
        {
            await File.WriteAllTextAsync(destination, "old content that is longer", token);
            var replacement = await storage.DownloadAsync(
                new DownloadOptions { FileName = "dir/file.txt", LocalPath = destination }, token);
            replacement.IsSuccess.ShouldBeTrue();
            await using var file = replacement.Value;
            (await file.ReadAllTextAsync(token)).ShouldBe("storage payload");
        }
        finally
        {
            File.Delete(destination);
        }

        var existsBeforeDelete = await storage.ExistsAsync("dir/file.txt", TestContext.Current.CancellationToken);
        existsBeforeDelete.IsSuccess.ShouldBeTrue();
        existsBeforeDelete.Value.ShouldBeTrue();

        var deleteDir = await storage.DeleteDirectoryAsync("dir", TestContext.Current.CancellationToken);
        deleteDir.IsSuccess.ShouldBeTrue();

        var existsAfterDelete = await storage.ExistsAsync("dir/file.txt", TestContext.Current.CancellationToken);
        existsAfterDelete.IsSuccess.ShouldBeTrue();
        existsAfterDelete.Value.ShouldBeFalse();
    }
}
