using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Threading.Tasks;
using ManagedCode.Communication;
using ManagedCode.Storage.Client;
using ManagedCode.Storage.Core.Helpers;
using ManagedCode.Storage.Core.Models;
using ManagedCode.Storage.Server.Controllers;
using Shouldly;
using Xunit;

namespace ManagedCode.Storage.Tests.AspNetTests.Runtime;

public sealed class StorageControllerRuntimeTests(StorageHttpRuntimeHost host) : IClassFixture<StorageHttpRuntimeHost>
{
    private const string Root = "runtime/storage/";

    [Theory]
    [InlineData(0, 3)]
    [InlineData(1, 4096)]
    [InlineData(2, 4096)]
    [InlineData(3, 4096)]
    public async Task UploadOverloads_AndAllDownloadRoutes_PreserveBytes(int overload, int length)
    {
        var bytes = new byte[length];
        Random.Shared.NextBytes(bytes);
        var client = new StorageClient(host.Client);
        var token = TestContext.Current.CancellationToken;
        Result<BlobMetadata> result;
        if (overload == 0) result = await client.UploadFile(bytes, Root + "upload", "file", token);
        else if (overload == 1) result = await client.UploadFile(Convert.ToBase64String(bytes), Root + "upload", "file", token);
        else if (overload == 2)
        {
            await using var local = await LocalFile.FromStreamAsync(new MemoryStream(bytes), token);
            result = await client.UploadFile(local.FileInfo, Root + "upload", "file", token);
        }
        else
        {
            using var stream = new MemoryStream(bytes);
            result = await client.UploadFile(stream, Root + "upload", "file", token);
        }
        result.IsSuccess.ShouldBeTrue(result.Problem?.ToString());
        var name = result.Value!.FullName!;
        foreach (var route in new[] { "download", "stream", "download-bytes" })
        {
            using var response = await host.Client.GetAsync(Root + route + "/" + name, token);
            response.StatusCode.ShouldBe(HttpStatusCode.OK);
            (await response.Content.ReadAsByteArrayAsync(token)).ShouldBe(bytes);
        }
        var downloaded = await client.DownloadFile(name, Root + "download", cancellationToken: token);
        downloaded.IsSuccess.ShouldBeTrue();
        await using var file = downloaded.Value!;
        (await file.ReadAllBytesAsync(token)).ShouldBe(bytes);
        var opened = await client.GetFileStream(name, Root + "stream", token);
        opened.IsSuccess.ShouldBeTrue();
        await using var openedStream = opened.Value!;
        using var output = new MemoryStream();
        await openedStream.CopyToAsync(output, token);
        output.ToArray().ShouldBe(bytes);
    }

    [Fact]
    public async Task RawUpload_AndHttpRanges_PreserveRequestedBytes()
    {
        var bytes = new byte[1234];
        Random.Shared.NextBytes(bytes);
        var name = Guid.NewGuid().ToString("N") + ".bin";
        var token = TestContext.Current.CancellationToken;
        using var request = new HttpRequestMessage(HttpMethod.Post, Root + "upload/stream") { Content = new ByteArrayContent(bytes) };
        request.Headers.Add(StorageServerHeaders.FileName, name);
        request.Headers.Add(StorageServerHeaders.ContentType, "application/octet-stream");
        request.Headers.Add(StorageServerHeaders.Directory, "nested");
        using var uploaded = await host.Client.SendAsync(request, token);
        (await uploaded.Content.ReadFromJsonAsync<Result<BlobMetadata>>(cancellationToken: token))!.IsSuccess.ShouldBeTrue();
        foreach (var route in new[] { "download", "stream", "download-bytes" })
        {
            using var range = new HttpRequestMessage(HttpMethod.Get, Root + route + "/nested/" + name);
            range.Headers.Range = new RangeHeaderValue(3, 17);
            using var response = await host.Client.SendAsync(range, token);
            response.StatusCode.ShouldBe(HttpStatusCode.PartialContent);
            (await response.Content.ReadAsByteArrayAsync(token)).ShouldBe(bytes[3..18]);
        }
    }

    [Fact]
    public async Task ChunkUpload_CompletesWithMatchingChecksumAndPersistedBytes()
    {
        var bytes = new byte[9237];
        Random.Shared.NextBytes(bytes);
        var client = new StorageClient(host.Client);
        client.SetChunkSize(1024);
        double progress = 0;
        using var stream = new MemoryStream(bytes);
        var completed = await client.UploadLargeFile(stream, Root + "upload-chunks/upload", Root + "upload-chunks/complete",
            value => progress = value, TestContext.Current.CancellationToken);
        completed.IsSuccess.ShouldBeTrue(System.Text.Json.JsonSerializer.Serialize(completed.Problem));
        completed.Value.ShouldBe(Crc32Helper.Calculate(bytes));
        progress.ShouldBe(100);
        var found = false;
        await foreach (var metadata in host.Storage.GetBlobMetadataListAsync(cancellationToken: TestContext.Current.CancellationToken))
        {
            if (metadata.Length != (ulong)bytes.Length) continue;
            var downloaded = await host.Storage.DownloadAsync(metadata.FullName!, TestContext.Current.CancellationToken);
            await using var file = downloaded.Value!;
            (await file.ReadAllBytesAsync(TestContext.Current.CancellationToken)).ShouldBe(bytes);
            found = true;
        }
        found.ShouldBeTrue();
    }

    [Fact]
    public async Task InvalidUploads_MissingDownloadsAndAbort_ReportErrors()
    {
        var token = TestContext.Current.CancellationToken;
        var client = new StorageClient(host.Client);
        var oversized = await client.UploadFile(new byte[16 * 1024 + 1], Root + "upload", "file", token);
        oversized.IsFailed.ShouldBeTrue();
        oversized.Problem!.StatusCode.ShouldBe(413);
        using var request = new HttpRequestMessage(HttpMethod.Post, Root + "upload/stream") { Content = new ByteArrayContent(new byte[1]) };
        using var missingHeader = await host.Client.SendAsync(request, token);
        (await missingHeader.Content.ReadFromJsonAsync<Result<BlobMetadata>>(cancellationToken: token))!.IsFailed.ShouldBeTrue();
        foreach (var route in new[] { "download", "stream", "download-bytes" })
        {
            using var response = await host.Client.GetAsync(Root + route + "/missing", token);
            response.IsSuccessStatusCode.ShouldBeFalse();
        }
        (await client.GetFileStream("missing", Root + "stream", token)).IsFailed.ShouldBeTrue();
        (await client.DownloadFile("missing", Root + "download", cancellationToken: token)).IsFailed.ShouldBeTrue();
        using var invalidChunk = new MultipartFormDataContent();
        invalidChunk.Add(new ByteArrayContent(new byte[1]), "File", "file");
        using var chunkResponse = await host.Client.PostAsync(Root + "upload-chunks/upload", invalidChunk, token);
        (await chunkResponse.Content.ReadFromJsonAsync<Result>(cancellationToken: token))!.IsFailed.ShouldBeTrue();
        using var aborted = await host.Client.DeleteAsync(Root + "upload-chunks/absent", token);
        aborted.StatusCode.ShouldBe(HttpStatusCode.NoContent);
    }
}
