using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Google.Cloud.Storage.V1;
using ManagedCode.Storage.Tests.Common;
using Shouldly;
using Xunit;

namespace ManagedCode.Storage.Tests.Storages.GCS;

public class GcsContainerStartupContractsTests
{
    [Theory]
    [InlineData("memory")]
    [InlineData("filesystem")]
    public async Task ConcurrentStartup_UsesCompleteScriptsAndCorrectExternalUploadEndpoints(string backend)
    {
        var token = TestContext.Current.CancellationToken;
        var containers = Enumerable.Range(0, 3).Select(_ => GcsContainerFactory.Create(backend)).ToArray();
        try
        {
            await Task.WhenAll(containers.Select(async container =>
            {
                await container.StartAsync(token);
                var client = new StorageClientBuilder { BaseUri = container.GetConnectionString(), UnauthenticatedAccess = true }.Build();
                var bucket = "startup-" + Guid.NewGuid().ToString("N");
                await client.CreateBucketAsync("storage-tests", bucket, cancellationToken: token);
                var bytes = Encoding.UTF8.GetBytes("Complete GCS startup: точні байти " + bucket);
                await using var input = new MemoryStream(bytes);
                await client.UploadObjectAsync(bucket, "proof.bin", "application/octet-stream", input, cancellationToken: token);
                await using var output = new MemoryStream();
                await client.DownloadObjectAsync(bucket, "proof.bin", output, cancellationToken: token);
                output.ToArray().ShouldBe(bytes);
            }));
        }
        finally
        {
            await Task.WhenAll(containers.Select(container => container.DisposeAsync().AsTask()));
        }
    }
}
