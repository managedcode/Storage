using System.IO;
using System.Threading.Tasks;
using ManagedCode.Storage.Client;
using Shouldly;
using Xunit;

namespace ManagedCode.Storage.Tests.AspNetTests.Runtime;

public sealed class HttpChunkRejectionTests(StorageHttpRuntimeHost host) : IClassFixture<StorageHttpRuntimeHost>
{
    [Fact]
    public async Task OversizedChunk_ReturnsTheServerRejectionWithoutCompletingAnUpload()
    {
        var client = new StorageClient(host.Client);
        client.SetChunkSize(8193);
        using var stream = new MemoryStream(new byte[8193]);
        var result = await client.UploadLargeFile(stream, "runtime/storage/upload-chunks/upload",
            "runtime/storage/upload-chunks/complete", null, TestContext.Current.CancellationToken);
        result.IsFailed.ShouldBeTrue();
        result.Problem!.StatusCode.ShouldBe(413);
    }
}
