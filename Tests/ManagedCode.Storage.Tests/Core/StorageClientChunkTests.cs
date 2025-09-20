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
using FluentAssertions;
using ManagedCode.Storage.Client;
using ManagedCode.Storage.Core.Helpers;
using ManagedCode.Storage.Core.Models;
using Xunit;

namespace ManagedCode.Storage.Tests.Core;

public class StorageClientChunkTests
{
    private const string UploadUrl = "https://localhost/upload";
    private const string CompleteUrl = "https://localhost/complete";

    [Fact]
    public async Task UploadLargeFile_WhenServerReturnsObject_ShouldParseChecksum()
    {
        var payload = CreatePayload(sizeInBytes: 5 * 1024 * 1024 + 123); // Ensure multiple chunks.
        var expectedChecksum = Crc32Helper.Calculate(payload);

        using var handler = new RecordingHandler(request =>
        {
            if (request.RequestUri!.AbsoluteUri == UploadUrl)
            {
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
            }

            if (request.RequestUri!.AbsoluteUri == CompleteUrl)
            {
                var json = JsonSerializer.Serialize(new
                {
                    isSuccess = true,
                    value = new
                    {
                        checksum = expectedChecksum,
                        metadata = (BlobMetadata?)null
                    }
                });

                return Task.FromResult(CreateJsonResponse(HttpStatusCode.OK, json));
            }

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
        });

        using var httpClient = new HttpClient(handler);
        var client = new StorageClient(httpClient);
        client.SetChunkSize(2 * 1024 * 1024);

        double? finalProgress = null;
        var progressEvents = new List<double>();
        var result = await client.UploadLargeFile(new MemoryStream(payload, writable: false), UploadUrl, CompleteUrl, progress =>
        {
            progressEvents.Add(progress);
            finalProgress = progress;
        });

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(expectedChecksum);
        handler.Requests.Should().HaveCount(4); // 3 chunks + completion.
        finalProgress.Should().Be(100d);
        progressEvents.Should().NotBeEmpty();
    }

    [Fact]
    public async Task UploadLargeFile_WhenServerReturnsNumber_ShouldParseChecksum()
    {
        var payload = CreatePayload(sizeInBytes: 1024 * 1024);
        var expectedChecksum = Crc32Helper.Calculate(payload);

        using var handler = new RecordingHandler(request =>
        {
            if (request.RequestUri!.AbsoluteUri == UploadUrl)
            {
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
            }

            if (request.RequestUri!.AbsoluteUri == CompleteUrl)
            {
                var json = JsonSerializer.Serialize(new
                {
                    isSuccess = true,
                    value = expectedChecksum
                });

                return Task.FromResult(CreateJsonResponse(HttpStatusCode.OK, json));
            }

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
        });

        using var httpClient = new HttpClient(handler);
        var client = new StorageClient(httpClient);
        client.SetChunkSize(256 * 1024);

        var result = await client.UploadLargeFile(new MemoryStream(payload, writable: false), UploadUrl, CompleteUrl, null);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(expectedChecksum);
    }

    [Fact]
    public async Task UploadLargeFile_WhenServerReturnsStringChecksum_ShouldParseChecksum()
    {
        var payload = CreatePayload(sizeInBytes: 256 * 1024);
        var expectedChecksum = Crc32Helper.Calculate(payload);

        using var handler = new RecordingHandler(request =>
        {
            if (request.RequestUri!.AbsoluteUri == UploadUrl)
            {
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
            }

            if (request.RequestUri!.AbsoluteUri == CompleteUrl)
            {
                var json = JsonSerializer.Serialize(new
                {
                    isSuccess = true,
                    value = expectedChecksum.ToString()
                });

                return Task.FromResult(CreateJsonResponse(HttpStatusCode.OK, json));
            }

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
        });

        using var httpClient = new HttpClient(handler);
        var client = new StorageClient(httpClient)
        {
            ChunkSize = 128 * 1024
        };

        var result = await client.UploadLargeFile(new MemoryStream(payload, writable: false), UploadUrl, CompleteUrl, null);
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(expectedChecksum);
    }

    [Fact]
    public async Task UploadLargeFile_WhenValueMissing_ShouldFail()
    {
        var payload = CreatePayload(sizeInBytes: 128 * 1024);

        using var handler = new RecordingHandler(request =>
        {
            if (request.RequestUri!.AbsoluteUri == UploadUrl)
            {
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
            }

            if (request.RequestUri!.AbsoluteUri == CompleteUrl)
            {
                const string json = "{\"isSuccess\":true}";
                return Task.FromResult(CreateJsonResponse(HttpStatusCode.OK, json));
            }

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
        });

        using var httpClient = new HttpClient(handler);
        var client = new StorageClient(httpClient)
        {
            ChunkSize = 64 * 1024
        };

        var result = await client.UploadLargeFile(new MemoryStream(payload, writable: false), UploadUrl, CompleteUrl, null);
        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public void UploadLargeFile_WhenChunkSizeMissing_ShouldThrow()
    {
        using var httpClient = new HttpClient(new RecordingHandler(_ => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK))));
        var client = new StorageClient(httpClient);

        Func<Task> act = () => client.UploadLargeFile(new MemoryStream(new byte[1]), UploadUrl, CompleteUrl, null);

        act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task UploadLargeFile_WhenServerReturnsZero_ShouldUseComputedChecksum()
    {
        var payload = CreatePayload(sizeInBytes: 512 * 1024);
        var expectedChecksum = Crc32Helper.Calculate(payload);

        using var handler = new RecordingHandler(request =>
        {
            if (request.RequestUri!.AbsoluteUri == UploadUrl)
            {
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
            }

            if (request.RequestUri!.AbsoluteUri == CompleteUrl)
            {
                const string json = "{\"isSuccess\":true,\"value\":0}";
                return Task.FromResult(CreateJsonResponse(HttpStatusCode.OK, json));
            }

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
        });

        using var httpClient = new HttpClient(handler);
        var client = new StorageClient(httpClient)
        {
            ChunkSize = 128 * 1024
        };

        var result = await client.UploadLargeFile(new MemoryStream(payload, writable: false), UploadUrl, CompleteUrl, null);
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(expectedChecksum);
    }

    private static byte[] CreatePayload(int sizeInBytes)
    {
        var data = new byte[sizeInBytes];
        for (var i = 0; i < data.Length; i++)
        {
            data[i] = (byte)(i % 255);
        }

        return data;
    }

    private static HttpResponseMessage CreateJsonResponse(HttpStatusCode statusCode, string json)
    {
        return new HttpResponseMessage(statusCode)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };
    }

    private sealed class RecordingHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, Task<HttpResponseMessage>> _handler;

        public RecordingHandler(Func<HttpRequestMessage, Task<HttpResponseMessage>> handler)
        {
            _handler = handler;
        }

        public List<HttpRequestMessage> Requests { get; } = new();

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Requests.Add(CloneRequest(request));
            return await _handler(request);
        }

        private static HttpRequestMessage CloneRequest(HttpRequestMessage original)
        {
            var clone = new HttpRequestMessage(original.Method, original.RequestUri);
            foreach (var header in original.Headers)
            {
                clone.Headers.TryAddWithoutValidation(header.Key, header.Value);
            }

            // Content is not required for current assertions; avoid buffering unnecessarily.
            return clone;
        }
    }
}
