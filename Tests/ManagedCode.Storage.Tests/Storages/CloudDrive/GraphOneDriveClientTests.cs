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
using Azure;
using Azure.Core;
using ManagedCode.Storage.OneDrive.Clients;
using Microsoft.Graph;
using Microsoft.Graph.Models;
using Shouldly;
using Xunit;

namespace ManagedCode.Storage.Tests.Storages.CloudDrive;

public class GraphOneDriveClientTests
{
    private const string RootKey = "root";

    [Fact]
    public async Task GraphClient_EndToEnd()
    {
        var handler = new FakeGraphHandler();
        var client = CreateGraphClient(handler);
        var storageClient = new GraphOneDriveClient(client);

        await storageClient.EnsureRootAsync("me", "work", true, CancellationToken.None);

        await using (var uploadStream = new MemoryStream(Encoding.UTF8.GetBytes("graph payload")))
        {
            var uploaded = await storageClient.UploadAsync("me", "work/doc.txt", uploadStream, "text/plain", CancellationToken.None);
            uploaded.Name.ShouldBe("doc.txt");
        }

        (await storageClient.ExistsAsync("me", "work/doc.txt", CancellationToken.None)).ShouldBeTrue();

        var metadata = await storageClient.GetMetadataAsync("me", "work/doc.txt", CancellationToken.None);
        metadata.ShouldNotBeNull();
        metadata!.Size.ShouldBe((long)"graph payload".Length);

        await using (var downloaded = await storageClient.DownloadAsync("me", "work/doc.txt", CancellationToken.None))
        using (var reader = new StreamReader(downloaded))
        {
            (await reader.ReadToEndAsync()).ShouldBe("graph payload");
        }

        var listed = new List<DriveItem>();
        await foreach (var item in storageClient.ListAsync("me", "work", CancellationToken.None))
        {
            listed.Add(item);
        }

        listed.ShouldContain(i => i.Name == "doc.txt");

        (await storageClient.DeleteAsync("me", "work/doc.txt", CancellationToken.None)).ShouldBeTrue();
        (await storageClient.ExistsAsync("me", "work/doc.txt", CancellationToken.None)).ShouldBeFalse();
    }

    private static GraphServiceClient CreateGraphClient(HttpMessageHandler handler)
    {
        var scopes = new[] { "https://graph.microsoft.com/.default" };
        var credential = new FakeTokenCredential();
        var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://graph.microsoft.com/v1.0")
        };

        return new GraphServiceClient(httpClient, credential, scopes, "https://graph.microsoft.com/v1.0");
    }

    private sealed class FakeTokenCredential : TokenCredential
    {
        public override AccessToken GetToken(TokenRequestContext requestContext, CancellationToken cancellationToken)
        {
            return new AccessToken("test-token", DateTimeOffset.UtcNow.AddHours(1));
        }

        public override ValueTask<AccessToken> GetTokenAsync(TokenRequestContext requestContext, CancellationToken cancellationToken)
        {
            return new ValueTask<AccessToken>(GetToken(requestContext, cancellationToken));
        }
    }

    private sealed class FakeGraphHandler : HttpMessageHandler
    {
        private readonly Dictionary<string, GraphEntry> _entries = new(StringComparer.OrdinalIgnoreCase)
        {
            [RootKey] = GraphEntry.Root()
        };

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (IsRootRequest(request.RequestUri))
            {
                return Task.FromResult(JsonResponse(_entries[RootKey]));
            }

            if (TryHandleChildrenRequest(request, out var childrenResponse))
            {
                return Task.FromResult(childrenResponse);
            }

            if (TryHandleItemRequest(request, out var itemResponse))
            {
                return Task.FromResult(itemResponse);
            }

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
        }

        private bool TryHandleItemRequest(HttpRequestMessage request, out HttpResponseMessage response)
        {
            response = new HttpResponseMessage(HttpStatusCode.NotFound);
            var segments = request.RequestUri!.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            var contentSegment = segments.FirstOrDefault(s => s.EndsWith(":content", StringComparison.OrdinalIgnoreCase));

            if (contentSegment != null)
            {
                var path = DecodePath(contentSegment.Replace(":content", string.Empty, StringComparison.OrdinalIgnoreCase));
                return HandleContentRequest(request, path, ref response);
            }

            var itemWithPath = segments.FirstOrDefault(s => s.Contains(':'));
            if (itemWithPath != null)
            {
                var path = DecodePath(itemWithPath.Trim(':'));
                return HandleMetadataRequest(request.Method, path, ref response);
            }

            return false;
        }

        private bool HandleMetadataRequest(HttpMethod method, string path, ref HttpResponseMessage response)
        {
            var entry = _entries.Values.FirstOrDefault(v => string.Equals(v.Path, path, StringComparison.OrdinalIgnoreCase));
            if (method == HttpMethod.Delete)
            {
                if (entry == null)
                {
                    response = new HttpResponseMessage(HttpStatusCode.NotFound);
                    return true;
                }

                _entries.Remove(entry.Id);
                response = new HttpResponseMessage(HttpStatusCode.NoContent);
                return true;
            }

            if (entry == null)
            {
                response = new HttpResponseMessage(HttpStatusCode.NotFound);
                return true;
            }

            response = JsonResponse(entry);
            return true;
        }

        private bool HandleContentRequest(HttpRequestMessage request, string path, ref HttpResponseMessage response)
        {
            if (request.Method == HttpMethod.Put)
            {
                var parentPath = Path.GetDirectoryName(path)?.Replace("\\", "/").Trim('/') ?? string.Empty;
                EnsureFolder(parentPath);

                var buffer = request.Content!.ReadAsStream();
                using var memory = new MemoryStream();
                buffer.CopyTo(memory);
                var entry = GraphEntry.File(Path.GetFileName(path), parentPath, memory.ToArray());
                _entries[entry.Id] = entry;
                response = JsonResponse(entry);
                return true;
            }

            var existing = _entries.Values.FirstOrDefault(v => string.Equals(v.Path, path, StringComparison.OrdinalIgnoreCase));
            if (existing == null)
            {
                response = new HttpResponseMessage(HttpStatusCode.NotFound);
                return true;
            }

            response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new ByteArrayContent(existing.Content)
            };

            return true;
        }

        private bool TryHandleChildrenRequest(HttpRequestMessage request, out HttpResponseMessage response)
        {
            response = new HttpResponseMessage(HttpStatusCode.NotFound);
            var path = request.RequestUri!.AbsolutePath;
            if (!path.EndsWith("/children", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            var idSegment = path.Contains("items", StringComparison.OrdinalIgnoreCase)
                ? path.Split('/', StringSplitOptions.RemoveEmptyEntries).SkipWhile(s => !s.Equals("items", StringComparison.OrdinalIgnoreCase)).Skip(1).FirstOrDefault()
                : RootKey;

            if (request.Method == HttpMethod.Post)
            {
                var body = request.Content!.ReadAsStringAsync().GetAwaiter().GetResult();
                var item = JsonSerializer.Deserialize<DriveItem>(body, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                var created = GraphEntry.Folder(item!.Name!, parentPath: _entries[idSegment ?? RootKey].Path);
                _entries[created.Id] = created;
                response = JsonResponse(created, HttpStatusCode.Created);
                return true;
            }

            var children = _entries.Values.Where(e => string.Equals(e.ParentPath, _entries[idSegment ?? RootKey].Path, StringComparison.OrdinalIgnoreCase)).ToList();
            response = JsonResponse(new DriveItemCollectionResponse
            {
                Value = children.Select(GraphEntry.ToDriveItem).ToList()
            });

            return true;
        }

        private static bool IsRootRequest(Uri? requestUri)
        {
            return requestUri != null && requestUri.AbsolutePath.TrimEnd('/').EndsWith("me/drive/root", StringComparison.OrdinalIgnoreCase);
        }

        private void EnsureFolder(string path)
        {
            var normalized = path.Trim('/');
            if (string.IsNullOrWhiteSpace(normalized))
            {
                return;
            }

            if (_entries.Values.Any(e => string.Equals(e.Path, normalized, StringComparison.OrdinalIgnoreCase)))
            {
                return;
            }

            var parentPath = Path.GetDirectoryName(normalized)?.Replace("\\", "/").Trim('/') ?? string.Empty;
            EnsureFolder(parentPath);

            var folder = GraphEntry.Folder(Path.GetFileName(normalized), parentPath);
            _entries[folder.Id] = folder;
        }

        private static string DecodePath(string segment)
        {
            return Uri.UnescapeDataString(segment.Replace("root:", string.Empty, StringComparison.OrdinalIgnoreCase)).Trim('/');
        }

        private static HttpResponseMessage JsonResponse(object content, HttpStatusCode status = HttpStatusCode.OK)
        {
            var response = new HttpResponseMessage(status)
            {
                Content = new StringContent(JsonSerializer.Serialize(content))
            };

            response.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");
            return response;
        }
    }

    private sealed class GraphEntry
    {
        public required string Id { get; init; }
        public required string Name { get; init; }
        public required string Path { get; init; }
        public required string ParentPath { get; init; }
        public byte[] Content { get; init; } = Array.Empty<byte>();
        public bool IsFolder { get; init; }

        public static GraphEntry Root()
        {
            return new GraphEntry
            {
                Id = RootKey,
                Name = "root",
                Path = string.Empty,
                ParentPath = string.Empty,
                IsFolder = true
            };
        }

        public static GraphEntry Folder(string name, string parentPath)
        {
            var normalizedParent = parentPath.Trim('/');
            var path = string.IsNullOrWhiteSpace(normalizedParent) ? name : $"{normalizedParent}/{name}";
            return new GraphEntry
            {
                Id = Guid.NewGuid().ToString("N"),
                Name = name,
                Path = path,
                ParentPath = normalizedParent,
                IsFolder = true
            };
        }

        public static GraphEntry File(string name, string parentPath, byte[] content)
        {
            var normalizedParent = parentPath.Trim('/');
            var path = string.IsNullOrWhiteSpace(normalizedParent) ? name : $"{normalizedParent}/{name}";
            return new GraphEntry
            {
                Id = Guid.NewGuid().ToString("N"),
                Name = name,
                Path = path,
                ParentPath = normalizedParent,
                Content = content,
                IsFolder = false
            };
        }

        public static DriveItem ToDriveItem(GraphEntry entry)
        {
            return new DriveItem
            {
                Id = entry.Id,
                Name = entry.Name,
                Size = entry.Content.LongLength,
                CreatedDateTime = DateTimeOffset.UtcNow,
                LastModifiedDateTime = DateTimeOffset.UtcNow,
                File = entry.IsFolder ? null : new Microsoft.Graph.Models.FileObject(),
                Folder = entry.IsFolder ? new Folder() : null
            };
        }
    }
}
