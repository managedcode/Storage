using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ManagedCode.Storage.Client;
using Shouldly;
using Xunit;

namespace ManagedCode.Storage.Tests.Core;

public class StorageClientDownloadTests
{
    [Fact]
    public async Task DownloadFile_DefaultDestinationKeepsSameNamedDownloadsIndependent()
    {
        using var handler = new DownloadHandler();
        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://localhost") };
        var client = new StorageClient(httpClient);

        var firstResult = await client.DownloadFile("shared.bin", "/download");
        firstResult.IsSuccess.ShouldBeTrue();
        await using var first = firstResult.Value!;

        var secondResult = await client.DownloadFile("shared.bin", "/download");
        secondResult.IsSuccess.ShouldBeTrue();
        await using var second = secondResult.Value!;

        first.FilePath.ShouldNotBe(second.FilePath);
        (await File.ReadAllTextAsync(first.FilePath)).ShouldBe("first payload");
        (await File.ReadAllTextAsync(second.FilePath)).ShouldBe("second payload");
    }

    private sealed class DownloadHandler : HttpMessageHandler
    {
        private int _requests;

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            request.RequestUri!.AbsolutePath.ShouldBe("/download/shared.bin");
            var payload = Interlocked.Increment(ref _requests) == 1 ? "first payload" : "second payload";
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new ByteArrayContent(Encoding.UTF8.GetBytes(payload))
            });
        }
    }
}
