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
using Google.Apis.Drive.v3;
using Google.Apis.Drive.v3.Data;
using Google.Apis.Http;
using Google.Apis.Services;
using ManagedCode.Storage.GoogleDrive.Clients;
using Shouldly;
using Xunit;
using DriveFile = Google.Apis.Drive.v3.Data.File;

namespace ManagedCode.Storage.Tests.Storages.CloudDrive;

public class GoogleDriveClientHttpTests
{
    private const string RootFolderId = "root";

    [Fact]
    public async Task GoogleDriveClient_WithHttpHandler_RoundTrip()
    {
        var handler = new FakeGoogleDriveHttpHandler();
        var driveService = CreateDriveService(handler);
        var client = new GoogleDriveClient(driveService);

        await client.EnsureRootAsync(RootFolderId, true, CancellationToken.None);

        await using (var uploadStream = new MemoryStream(Encoding.UTF8.GetBytes("google payload")))
        {
            var uploaded = await client.UploadAsync(RootFolderId, "dir/file.txt", uploadStream, "text/plain", CancellationToken.None);
            uploaded.Name.ShouldBe("file.txt");
            uploaded.Size.ShouldBe("google payload".Length);
        }

        await using (var nestedStream = new MemoryStream(Encoding.UTF8.GetBytes("nested payload")))
        {
            var uploaded = await client.UploadAsync(RootFolderId, "dir/sub/inner.txt", nestedStream, "text/plain", CancellationToken.None);
            uploaded.Name.ShouldBe("inner.txt");
        }

        (await client.ExistsAsync(RootFolderId, "dir/file.txt", CancellationToken.None)).ShouldBeTrue();
        (await client.ExistsAsync(RootFolderId, "dir/sub/inner.txt", CancellationToken.None)).ShouldBeTrue();

        await using (var downloaded = await client.DownloadAsync(RootFolderId, "dir/file.txt", CancellationToken.None))
        using (var reader = new StreamReader(downloaded, Encoding.UTF8))
        {
            (await reader.ReadToEndAsync()).ShouldBe("google payload");
        }

        var listed = new List<DriveFile>();
        await foreach (var item in client.ListAsync(RootFolderId, "dir", CancellationToken.None))
        {
            listed.Add(item);
        }

        listed.ShouldContain(f => f.Name == "file.txt");

        (await client.DeleteAsync(RootFolderId, "dir", CancellationToken.None)).ShouldBeTrue();
        (await client.ExistsAsync(RootFolderId, "dir/file.txt", CancellationToken.None)).ShouldBeFalse();
        (await client.ExistsAsync(RootFolderId, "dir/sub/inner.txt", CancellationToken.None)).ShouldBeFalse();

        var afterDelete = new List<DriveFile>();
        await foreach (var item in client.ListAsync(RootFolderId, "dir", CancellationToken.None))
        {
            afterDelete.Add(item);
        }

        afterDelete.ShouldBeEmpty();
        (await client.DeleteAsync(RootFolderId, "dir", CancellationToken.None)).ShouldBeFalse();
    }

    private static DriveService CreateDriveService(HttpMessageHandler handler)
    {
        return new DriveService(new BaseClientService.Initializer
        {
            ApplicationName = "ManagedCode.Storage.Tests",
            HttpClientInitializer = new NullCredentialInitializer(),
            GZipEnabled = false,
            HttpClientFactory = new HttpClientFromMessageHandlerFactory(_ =>
                new HttpClientFromMessageHandlerFactory.ConfiguredHttpMessageHandler(handler, false, false))
        });
    }

    private sealed class NullCredentialInitializer : IConfigurableHttpClientInitializer
    {
        public void Initialize(ConfigurableHttpClient httpClient)
        {
        }
    }

    private sealed class FakeGoogleDriveHttpHandler : HttpMessageHandler
    {
        private const string FolderMimeType = "application/vnd.google-apps.folder";
        private readonly Dictionary<string, Entry> _entriesById = new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<(string ParentId, string Name), string> _idByParentAndName = new();
        private readonly Dictionary<string, PendingUpload> _pendingUploads = new(StringComparer.OrdinalIgnoreCase);
        private int _counter;

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var path = request.RequestUri?.AbsolutePath ?? string.Empty;
            var query = ParseQuery(request.RequestUri?.Query);

            if (request.Method == HttpMethod.Get && path.Equals("/drive/v3/files", StringComparison.OrdinalIgnoreCase))
            {
                var q = query.TryGetValue("q", out var qValue) ? qValue : string.Empty;
                var files = List(q);
                return JsonResponse(new
                {
                    files = files.Select(ToResponse).ToList()
                });
            }

            if (request.Method == HttpMethod.Post && path.Equals("/drive/v3/files", StringComparison.OrdinalIgnoreCase))
            {
                var body = await request.Content!.ReadAsStringAsync(cancellationToken);
                var model = JsonSerializer.Deserialize<CreateRequest>(body, new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
                    ?? throw new InvalidOperationException("Create request body is missing.");

                var parentId = model.Parents?.FirstOrDefault() ?? RootFolderId;
                var mimeType = string.IsNullOrWhiteSpace(model.MimeType) ? "application/octet-stream" : model.MimeType;
                var created = CreateEntry(name: model.Name ?? Guid.NewGuid().ToString("N"), parentId: parentId, mimeType: mimeType, content: Array.Empty<byte>());
                return JsonResponse(ToResponse(created));
            }

            if (request.Method == HttpMethod.Post
                && path.Equals("/upload/drive/v3/files", StringComparison.OrdinalIgnoreCase)
                && query.TryGetValue("uploadType", out var uploadType)
                && string.Equals(uploadType, "resumable", StringComparison.OrdinalIgnoreCase))
            {
                var body = await request.Content!.ReadAsStringAsync(cancellationToken);
                var model = JsonSerializer.Deserialize<CreateRequest>(body, new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
                    ?? throw new InvalidOperationException("Upload initiation body is missing.");

                var uploadId = "upload-" + Interlocked.Increment(ref _counter);
                _pendingUploads[uploadId] = new PendingUpload(
                    Name: model.Name ?? Guid.NewGuid().ToString("N"),
                    ParentId: model.Parents?.FirstOrDefault() ?? RootFolderId,
                    MimeType: model.MimeType ?? "application/octet-stream");

                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Headers = { Location = new Uri($"https://www.googleapis.com/upload/drive/v3/files?uploadType=resumable&upload_id={uploadId}") },
                    Content = new ByteArrayContent(Array.Empty<byte>())
                };
            }

            if (request.Method == HttpMethod.Put
                && path.Equals("/upload/drive/v3/files", StringComparison.OrdinalIgnoreCase)
                && query.TryGetValue("uploadType", out uploadType)
                && string.Equals(uploadType, "resumable", StringComparison.OrdinalIgnoreCase)
                && query.TryGetValue("upload_id", out var uploadIdValue)
                && _pendingUploads.TryGetValue(uploadIdValue, out var pending))
            {
                var content = await request.Content!.ReadAsByteArrayAsync(cancellationToken);
                var created = CreateEntry(pending.Name, pending.ParentId, pending.MimeType, content);
                _pendingUploads.Remove(uploadIdValue);
                return JsonResponse(ToResponse(created));
            }

            if (path.StartsWith("/drive/v3/files/", StringComparison.OrdinalIgnoreCase))
            {
                var fileId = path["/drive/v3/files/".Length..];
                if (request.Method == HttpMethod.Delete)
                {
                    if (!_entriesById.Remove(fileId))
                    {
                        return new HttpResponseMessage(HttpStatusCode.NotFound);
                    }

                    return new HttpResponseMessage(HttpStatusCode.NoContent);
                }

                if (request.Method == HttpMethod.Get && query.TryGetValue("alt", out var alt) && string.Equals(alt, "media", StringComparison.OrdinalIgnoreCase))
                {
                    if (!_entriesById.TryGetValue(fileId, out var entry) || entry.MimeType == FolderMimeType)
                    {
                        return new HttpResponseMessage(HttpStatusCode.NotFound);
                    }

                    return new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new ByteArrayContent(entry.Content)
                        {
                            Headers = { ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(entry.MimeType) }
                        }
                    };
                }

                if (request.Method == HttpMethod.Get)
                {
                    if (!_entriesById.TryGetValue(fileId, out var entry))
                    {
                        return new HttpResponseMessage(HttpStatusCode.NotFound);
                    }

                    return JsonResponse(ToResponse(entry));
                }
            }

            return new HttpResponseMessage(HttpStatusCode.NotFound)
            {
                Content = new StringContent($"Unhandled Drive request: {request.Method} {request.RequestUri}")
            };
        }

        private Entry CreateEntry(string name, string parentId, string mimeType, byte[] content)
        {
            var id = "id-" + Interlocked.Increment(ref _counter);
            var entry = new Entry(
                Id: id,
                Name: name,
                ParentId: parentId,
                MimeType: mimeType,
                Content: content,
                Created: DateTimeOffset.UtcNow,
                Modified: DateTimeOffset.UtcNow);

            _entriesById[id] = entry;
            _idByParentAndName[(parentId, name)] = id;
            return entry;
        }

        private IEnumerable<Entry> List(string q)
        {
            if (string.IsNullOrWhiteSpace(q))
            {
                return Enumerable.Empty<Entry>();
            }

            var parentId = ExtractFirstQuoted(q);
            if (parentId == null)
            {
                return Enumerable.Empty<Entry>();
            }

            var name = ExtractNameClause(q);
            if (name != null)
            {
                return _idByParentAndName.TryGetValue((parentId, name), out var id) && _entriesById.TryGetValue(id, out var entry)
                    ? new[] { entry }
                    : Enumerable.Empty<Entry>();
            }

            return _entriesById.Values.Where(e => string.Equals(e.ParentId, parentId, StringComparison.OrdinalIgnoreCase));
        }

        private static string? ExtractFirstQuoted(string value)
        {
            var first = value.IndexOf('\'');
            if (first < 0)
            {
                return null;
            }

            var second = value.IndexOf('\'', first + 1);
            return second < 0 ? null : value.Substring(first + 1, second - first - 1);
        }

        private static string? ExtractNameClause(string q)
        {
            const string marker = "name='";
            var start = q.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
            if (start < 0)
            {
                return null;
            }

            start += marker.Length;
            var end = q.IndexOf('\'', start);
            return end < 0 ? null : q.Substring(start, end - start);
        }

        private static Dictionary<string, string> ParseQuery(string? query)
        {
            var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (string.IsNullOrWhiteSpace(query))
            {
                return result;
            }

            foreach (var part in query.TrimStart('?').Split('&', StringSplitOptions.RemoveEmptyEntries))
            {
                var kv = part.Split('=', 2);
                var key = Uri.UnescapeDataString(kv[0]);
                var value = kv.Length == 2 ? Uri.UnescapeDataString(kv[1]) : string.Empty;
                result[key] = value;
            }

            return result;
        }

        private HttpResponseMessage JsonResponse(object payload, HttpStatusCode statusCode = HttpStatusCode.OK)
        {
            var json = JsonSerializer.Serialize(payload, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
            var bytes = Encoding.UTF8.GetBytes(json);
            return new HttpResponseMessage(statusCode)
            {
                Content = new ByteArrayContent(bytes)
                {
                    Headers =
                    {
                        ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json"),
                        ContentLength = bytes.LongLength
                    }
                }
            };
        }

        private sealed record Entry(string Id, string Name, string ParentId, string MimeType, byte[] Content, DateTimeOffset Created, DateTimeOffset Modified);

        private sealed record PendingUpload(string Name, string ParentId, string MimeType);

        private sealed class CreateRequest
        {
            public string? Name { get; set; }
            public IList<string>? Parents { get; set; }
            public string? MimeType { get; set; }
        }

        private static object ToResponse(Entry entry)
        {
            return new
            {
                id = entry.Id,
                name = entry.Name,
                parents = new[] { entry.ParentId },
                mimeType = entry.MimeType,
                createdTime = entry.Created.ToString("O"),
                modifiedTime = entry.Modified.ToString("O"),
                size = entry.MimeType == FolderMimeType ? null : entry.Content.LongLength.ToString()
            };
        }
    }
}
