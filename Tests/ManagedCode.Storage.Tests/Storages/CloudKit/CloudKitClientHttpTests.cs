using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using ManagedCode.Storage.CloudKit.Clients;
using ManagedCode.Storage.CloudKit.Options;
using Shouldly;
using Xunit;

namespace ManagedCode.Storage.Tests.Storages.CloudKit;

public class CloudKitClientHttpTests
{
    [Fact]
    public async Task CloudKitClient_WithHttpHandler_RoundTrip()
    {
        var handler = new FakeCloudKitHttpHandler();
        var httpClient = new HttpClient(handler);

        var options = new CloudKitStorageOptions
        {
            ContainerId = "iCloud.com.example.app",
            Environment = CloudKitEnvironment.Development,
            Database = CloudKitDatabase.Public,
            ApiToken = "test-token",
            RecordType = "MCStorageFile",
            PathFieldName = "path",
            ContentTypeFieldName = "contentType",
            AssetFieldName = "file"
        };

        using var client = new CloudKitClient(options, httpClient);

        const string recordName = "record-1";
        const string internalPath = "app-data/dir/file.txt";

        await using (var uploadStream = new MemoryStream(Encoding.UTF8.GetBytes("cloudkit payload")))
        {
            var uploaded = await client.UploadAsync(recordName, internalPath, uploadStream, "text/plain", CancellationToken.None);
            uploaded.RecordName.ShouldBe(recordName);
            uploaded.Path.ShouldBe(internalPath);
            uploaded.ContentType.ShouldBe("text/plain");
            uploaded.Size.ShouldBe((ulong)"cloudkit payload".Length);
        }

        (await client.ExistsAsync(recordName, CancellationToken.None)).ShouldBeTrue();

        await using (var downloaded = await client.DownloadAsync(recordName, CancellationToken.None))
        using (var reader = new StreamReader(downloaded, Encoding.UTF8))
        {
            (await reader.ReadToEndAsync()).ShouldBe("cloudkit payload");
        }

        var listed = new List<CloudKitRecord>();
        await foreach (var record in client.QueryByPathPrefixAsync("app-data/dir/", CancellationToken.None))
        {
            listed.Add(record);
        }

        listed.ShouldContain(r => r.RecordName == recordName);

        (await client.DeleteAsync(recordName, CancellationToken.None)).ShouldBeTrue();
        (await client.ExistsAsync(recordName, CancellationToken.None)).ShouldBeFalse();
        (await client.DeleteAsync(recordName, CancellationToken.None)).ShouldBeFalse();
    }

    [Fact]
    public async Task CloudKitClient_WithWebAuthToken_ShouldRotateTokenAcrossRequests()
    {
        var handler = new FakeCloudKitHttpHandler();
        var httpClient = new HttpClient(handler);

        var options = new CloudKitStorageOptions
        {
            ContainerId = "iCloud.com.example.app",
            Environment = CloudKitEnvironment.Development,
            Database = CloudKitDatabase.Public,
            ApiToken = "test-token",
            WebAuthToken = "initial-web-token",
            RecordType = "MCStorageFile",
            PathFieldName = "path",
            ContentTypeFieldName = "contentType",
            AssetFieldName = "file"
        };

        using var client = new CloudKitClient(options, httpClient);

        await using (var uploadStream = new MemoryStream(Encoding.UTF8.GetBytes("cloudkit payload")))
        {
            _ = await client.UploadAsync("record-rotating", "app-data/rotating.txt", uploadStream, "text/plain", CancellationToken.None);
        }

        options.WebAuthToken.ShouldNotBeNull();
        options.WebAuthToken.ShouldNotBe("initial-web-token");
        options.WebAuthToken.ShouldStartWith("web-token-");

        (await client.ExistsAsync("record-rotating", CancellationToken.None)).ShouldBeTrue();
        options.WebAuthToken.ShouldStartWith("web-token-");
    }
}
