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
    private const string DriveKey = "drive";

    [Fact]
    public async Task GraphClient_EndToEnd()
    {
        var handler = new FakeGraphHandler();
        var client = CreateGraphClient(handler);
        var storageClient = new GraphOneDriveClient(client);

        await storageClient.EnsureRootAsync("me", "work", true, CancellationToken.None);

        var rootItems = new List<DriveItem>();
        await foreach (var item in storageClient.ListAsync("me", null, CancellationToken.None))
        {
            rootItems.Add(item);
        }

        rootItems.ShouldContain(i => i.Name == "work");

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
        (await storageClient.DeleteAsync("me", "work/doc.txt", CancellationToken.None)).ShouldBeFalse();
    }

    [Fact]
    public async Task EnsureRootAsync_WhenFolderMissing_AndCreateIfNotExistsFalse_ShouldThrow()
    {
        var handler = new FakeGraphHandler();
        var client = CreateGraphClient(handler);
        var storageClient = new GraphOneDriveClient(client);

        await Should.ThrowAsync<DirectoryNotFoundException>(() =>
            storageClient.EnsureRootAsync("me", "missing", false, CancellationToken.None));
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

            if (IsMeDriveRequest(request.RequestUri))
            {
                return Task.FromResult(JsonResponse(new Microsoft.Graph.Models.Drive { Id = DriveKey }));
            }

            if (IsRootRequest(request.RequestUri))
            {
                return Task.FromResult(JsonResponse(GraphEntry.ToDriveItem(_entries[RootKey])));
            }

            if (TryHandleChildrenRequest(request, out var childrenResponse))
            {
                return Task.FromResult(childrenResponse);
            }

            if (TryHandleItemRequest(request, out var itemResponse))
            {
                return Task.FromResult(itemResponse);
            }

            return Task.FromResult(NotFoundResponse($"Unhandled Graph request: {request.Method} {request.RequestUri}"));
        }

        private bool HandleMetadataRequest(HttpMethod method, string path, ref HttpResponseMessage response)
        {
            var entry = _entries.Values.FirstOrDefault(v => string.Equals(v.Path, path, StringComparison.OrdinalIgnoreCase));
            if (method == HttpMethod.Delete)
            {
                if (entry == null)
                {
                    response = NotFoundResponse();
                    return true;
                }

                _entries.Remove(entry.Id);
                response = new HttpResponseMessage(HttpStatusCode.NoContent);
                return true;
            }

            if (entry == null)
            {
                response = NotFoundResponse();
                return true;
            }

            response = JsonResponse(GraphEntry.ToDriveItem(entry));
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
                response = JsonResponse(GraphEntry.ToDriveItem(entry));
                return true;
            }

            var existing = _entries.Values.FirstOrDefault(v => string.Equals(v.Path, path, StringComparison.OrdinalIgnoreCase));
            if (existing == null)
            {
                response = NotFoundResponse();
                return true;
            }

            response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new ByteArrayContent(existing.Content)
            };

            return true;
        }

        private HttpResponseMessage NotFoundResponse(string? message = null)
        {
            return JsonResponse(new
            {
                error = new
                {
                    code = "itemNotFound",
                    message = message ?? "Item not found."
                }
            }, HttpStatusCode.NotFound);
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
                response = JsonResponse(GraphEntry.ToDriveItem(created), HttpStatusCode.Created);
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
            if (requestUri == null)
            {
                return false;
            }

            var path = requestUri.AbsolutePath.TrimEnd('/');
            return path.EndsWith("/me/drive/root", StringComparison.OrdinalIgnoreCase)
                   || path.EndsWith($"/drives/{DriveKey}/root", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsMeDriveRequest(Uri? requestUri)
        {
            return requestUri != null && requestUri.AbsolutePath.TrimEnd('/').EndsWith("/me/drive", StringComparison.OrdinalIgnoreCase);
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

        private static bool TryGetItemPath(Uri requestUri, out string path, out bool isContent)
        {
            path = string.Empty;
            isContent = false;

            var requestPath = Uri.UnescapeDataString(requestUri.AbsolutePath);
            var markerIndex = requestPath.IndexOf("/root:", StringComparison.OrdinalIgnoreCase);
            if (markerIndex < 0)
            {
                return false;
            }

            var itemPath = requestPath[(markerIndex + "/root:".Length)..];
            if (itemPath.EndsWith(":/content", StringComparison.OrdinalIgnoreCase))
            {
                isContent = true;
                itemPath = itemPath[..^":/content".Length];
            }

            itemPath = itemPath.TrimEnd(':');
            path = itemPath.Trim('/');
            return true;
        }

	        private static HttpResponseMessage JsonResponse(object content, HttpStatusCode status = HttpStatusCode.OK)
	        {
	            var json = JsonSerializer.Serialize(content, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
	            return new HttpResponseMessage(status)
	            {
	                Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json")
	            };
	        }

        private bool TryHandleItemRequest(HttpRequestMessage request, out HttpResponseMessage response)
        {
            response = new HttpResponseMessage(HttpStatusCode.NotFound);

            if (!TryGetItemPath(request.RequestUri!, out var path, out var isContent))
            {
                return false;
            }

            return isContent
                ? HandleContentRequest(request, path, ref response)
                : HandleMetadataRequest(request.Method, path, ref response);
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
