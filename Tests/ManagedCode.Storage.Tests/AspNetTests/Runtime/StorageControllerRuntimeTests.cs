using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Threading;
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

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task RawUpload_AndHttpRanges_PreserveRequestedBytes(bool chunked)
    {
        var bytes = new byte[1234];
        Random.Shared.NextBytes(bytes);
        var name = Guid.NewGuid().ToString("N") + ".bin";
        var token = TestContext.Current.CancellationToken;
        using var request = new HttpRequestMessage(HttpMethod.Post, Root + "upload/stream")
        {
            Content = chunked ? new StreamContent(new ShortReadStream(bytes, false)) : new ByteArrayContent(bytes)
        };
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

    [Theory]
    [InlineData(9237, false, true)]
    [InlineData(9238, true, true)]
    [InlineData(9239, true, false)]
    public async Task ChunkUpload_CompletesWithMatchingChecksumAndPersistedBytes(int length, bool shortReads, bool seekable)
    {
        var bytes = new byte[length];
        Random.Shared.NextBytes(bytes);
        var client = new StorageClient(host.Client);
        client.SetChunkSize(1024);
        double progress = 0;
        using var stream = shortReads ? new ShortReadStream(bytes, seekable) : new MemoryStream(bytes);
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

    private sealed class ShortReadStream(byte[] data, bool seekable) : MemoryStream(data)
    {
        public override bool CanSeek => seekable;

        public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default) =>
            base.ReadAsync(buffer[..Math.Min(buffer.Length, 113)], cancellationToken);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task RawUpload_RejectsOversizedBodyAndPreservesTheStoredFile(bool chunked)
    {
        var token = TestContext.Current.CancellationToken;
        var name = $"limit-{Guid.NewGuid():N}.bin";
        (await host.Storage.UploadAsync("original", new UploadOptions(name), token)).IsSuccess.ShouldBeTrue();
        var bytes = new byte[16 * 1024 + 1];
        using var request = new HttpRequestMessage(HttpMethod.Post, Root + "upload/stream")
        {
            Content = chunked ? new StreamContent(new ShortReadStream(bytes, false)) : new ByteArrayContent(bytes)
        };
        request.Headers.Add(StorageServerHeaders.FileName, name);

        using var response = await host.Client.SendAsync(request, token);
        var result = await response.Content.ReadFromJsonAsync<Result<BlobMetadata>>(cancellationToken: token);

        result!.IsFailed.ShouldBeTrue();
        result.Problem.StatusCode.ShouldBe(413);
        var stored = await host.Storage.DownloadAsync(name, token);
        stored.IsSuccess.ShouldBeTrue();
        await using var file = stored.Value.ShouldNotBeNull();
        (await file.ReadAllTextAsync(token)).ShouldBe("original");
    }

    [Fact]
    public async Task ChunkUpload_ReportsServerChecksumMismatch()
    {
        var client = new StorageClient(host.Client);
        client.SetChunkSize(1024);
        using var stream = new MemoryStream(new byte[23]);

        var result = await client.UploadLargeFile(stream, Root + "upload-chunks/upload",
            Root + "upload-chunks/wrong-checksum", null, TestContext.Current.CancellationToken);

        result.IsFailed.ShouldBeTrue();
        result.Problem.Title.ShouldNotBeNull().ShouldContain("checksum");
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
