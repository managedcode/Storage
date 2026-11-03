using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Amazon.S3;
using ManagedCode.Storage.Aws;
using ManagedCode.Storage.Core.Models;
using ManagedCode.Storage.Tests.Common;
using ManagedCode.Storage.Tests.Storages.Abstracts;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Testcontainers.LocalStack;
using Xunit;

namespace ManagedCode.Storage.Tests.Storages.AWS;

public class AWSDownloadTests : DownloadTests<LocalStackContainer>
{
    protected override LocalStackContainer Build()
    {
        return AwsContainerFactory.Create();
    }

    protected override ServiceProvider ConfigureServices()
    {
        return AWSConfigurator.ConfigureServices(Container.GetConnectionString());
    }

    [Fact]
    public async Task NestedDownload_ReadsBodyAndMetadataFromTheSameS3Request()
    {
        var token = TestContext.Current.CancellationToken;
        const string key = "nested/source.txt";
        var storage = Storage.ShouldBeOfType<AWSStorage>();
        (await storage.UploadAsync("content", new UploadOptions(key), token)).IsSuccess.ShouldBeTrue();
        var client = storage.StorageClient.ShouldBeOfType<AmazonS3Client>();
        var requests = 0;
        client.BeforeRequestEvent += (_, _) => Interlocked.Increment(ref requests);

        var downloaded = await storage.DownloadAsync(key, token);

        downloaded.IsSuccess.ShouldBeTrue();
        requests.ShouldBe(1);
        await using var file = downloaded.Value;
        file.BlobMetadata!.FullName.ShouldBe(key);
        (await file.ReadAllTextAsync(token)).ShouldBe("content");
        var metadata = await storage.GetBlobMetadataAsync(key, token);
        metadata.IsSuccess.ShouldBeTrue();
        metadata.Value.FullName.ShouldBe(key);
        var listed = await storage.GetBlobMetadataListAsync("nested/", token).ToListAsync(token);
        listed.Count.ShouldBe(1);
        listed[0].FullName.ShouldBe(key);
    }
}
