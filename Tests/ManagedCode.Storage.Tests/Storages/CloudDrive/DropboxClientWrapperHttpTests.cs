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
using Dropbox.Api;
using ManagedCode.Storage.Dropbox;
using ManagedCode.Storage.Dropbox.Clients;
using ManagedCode.Storage.Dropbox.Options;
using Shouldly;
using Xunit;

namespace ManagedCode.Storage.Tests.Storages.CloudDrive;

public class DropboxClientWrapperHttpTests
{
    [Fact]
    public async Task DropboxClientWrapper_WithHttpHandler_RoundTrip()
    {
        var handler = new FakeDropboxHttpHandler();
        var httpClient = new HttpClient(handler);
        var config = new DropboxClientConfig("ManagedCode.Storage.Tests")
        {
            HttpClient = httpClient,
            LongPollHttpClient = httpClient
        };

        using var dropboxClient = new DropboxClient("test-token", config);
        var wrapper = new DropboxClientWrapper(dropboxClient);

        await wrapper.EnsureRootAsync("/apps/demo", true, CancellationToken.None);

        await using (var uploadStream = new MemoryStream(Encoding.UTF8.GetBytes("dropbox payload")))
        {
            var uploaded = await wrapper.UploadAsync("/apps/demo", "file.json", uploadStream, "application/json", CancellationToken.None);
            uploaded.Name.ShouldBe("file.json");
            uploaded.Path.ShouldBe("/apps/demo/file.json");
        }

        (await wrapper.ExistsAsync("/apps/demo", "file.json", CancellationToken.None)).ShouldBeTrue();

        await using (var downloaded = await wrapper.DownloadAsync("/apps/demo", "file.json", CancellationToken.None))
        using (var reader = new StreamReader(downloaded, Encoding.UTF8))
        {
            (await reader.ReadToEndAsync()).ShouldBe("dropbox payload");
        }

        var items = new List<DropboxItemMetadata>();
        await foreach (var item in wrapper.ListAsync("/apps/demo", null, CancellationToken.None))
        {
            items.Add(item);
        }

        items.ShouldContain(i => i.Name == "file.json");

        (await wrapper.DeleteAsync("/apps/demo", "file.json", CancellationToken.None)).ShouldBeTrue();
        (await wrapper.DeleteAsync("/apps/demo", "file.json", CancellationToken.None)).ShouldBeFalse();
    }

    [Fact]
    public async Task DropboxStorage_WithAccessTokenAndHttpHandler_RoundTrip()
    {
        var handler = new FakeDropboxHttpHandler();
        var httpClient = new HttpClient(handler);
        var config = new DropboxClientConfig("ManagedCode.Storage.Tests")
        {
            HttpClient = httpClient,
            LongPollHttpClient = httpClient
        };

        using var storage = new DropboxStorage(new DropboxStorageOptions
        {
            RootPath = "/apps/demo",
            AccessToken = "test-token",
            DropboxClientConfig = config,
            CreateContainerIfNotExists = true
        });

        (await storage.UploadAsync("dropbox payload", options => options.FileName = "file.json")).IsSuccess.ShouldBeTrue();

        var exists = await storage.ExistsAsync("file.json");
        exists.IsSuccess.ShouldBeTrue();
        exists.Value.ShouldBeTrue();

        var download = await storage.DownloadAsync("file.json");
        download.IsSuccess.ShouldBeTrue();
        using (var reader = new StreamReader(download.Value.FileStream, Encoding.UTF8))
        {
            (await reader.ReadToEndAsync()).ShouldBe("dropbox payload");
        }
    }

    private sealed class FakeDropboxHttpHandler : HttpMessageHandler
    {
        private const string ApiHost = "api.dropboxapi.com";
        private const string ContentHost = "content.dropboxapi.com";

        private readonly Dictionary<string, Entry> _entries = new(StringComparer.OrdinalIgnoreCase);

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var host = request.RequestUri?.Host ?? string.Empty;
            var path = request.RequestUri?.AbsolutePath ?? string.Empty;

            if (host.Equals(ApiHost, StringComparison.OrdinalIgnoreCase))
            {
                return await HandleApiAsync(request, path, cancellationToken);
            }

            if (host.Equals(ContentHost, StringComparison.OrdinalIgnoreCase))
            {
                return await HandleContentAsync(request, path, cancellationToken);
            }

            return new HttpResponseMessage(HttpStatusCode.NotFound);
        }

        private async Task<HttpResponseMessage> HandleApiAsync(HttpRequestMessage request, string path, CancellationToken cancellationToken)
        {
            if (request.Method != HttpMethod.Post)
            {
                return new HttpResponseMessage(HttpStatusCode.MethodNotAllowed);
            }

            var body = request.Content == null ? string.Empty : await request.Content.ReadAsStringAsync(cancellationToken);
            var json = string.IsNullOrWhiteSpace(body) ? null : JsonDocument.Parse(body);

            if (path.Equals("/2/files/get_metadata", StringComparison.OrdinalIgnoreCase))
            {
                var metadataPath = ReadPath(json);
                if (!_entries.TryGetValue(NormalizeLower(metadataPath), out var entry))
                {
                    return PathNotFoundError();
                }

                return JsonResponse(ToMetadata(entry));
            }

            if (path.Equals("/2/files/create_folder_v2", StringComparison.OrdinalIgnoreCase))
            {
                var folderPath = ReadPath(json);
                var normalized = NormalizeDisplay(folderPath);
                var created = EnsureFolder(normalized);
                return JsonResponse(new Dictionary<string, object?>
                {
                    ["metadata"] = ToMetadata(created)
                });
            }

            if (path.Equals("/2/files/list_folder", StringComparison.OrdinalIgnoreCase))
            {
                var folderPath = ReadPath(json);
                var normalizedFolder = NormalizeLower(NormalizeDisplay(folderPath));
                var entries = ListChildren(normalizedFolder).Select(ToMetadata).ToList();

                return JsonResponse(new Dictionary<string, object?>
                {
                    ["entries"] = entries,
                    ["cursor"] = "cursor-1",
                    ["has_more"] = false
                });
            }

            if (path.Equals("/2/files/list_folder/continue", StringComparison.OrdinalIgnoreCase))
            {
                return JsonResponse(new Dictionary<string, object?>
                {
                    ["entries"] = Array.Empty<object>(),
                    ["cursor"] = "cursor-1",
                    ["has_more"] = false
                });
            }

            if (path.Equals("/2/files/delete_v2", StringComparison.OrdinalIgnoreCase))
            {
                var deletePath = ReadPath(json);
                var normalized = NormalizeLower(NormalizeDisplay(deletePath));
                var deleted = DeleteRecursive(normalized);
                if (deleted == null)
                {
                    return PathLookupNotFoundError();
                }

                return JsonResponse(new Dictionary<string, object?>
                {
                    ["metadata"] = ToMetadata(deleted)
                });
            }

            return new HttpResponseMessage(HttpStatusCode.NotFound);
        }

        private async Task<HttpResponseMessage> HandleContentAsync(HttpRequestMessage request, string path, CancellationToken cancellationToken)
        {
            if (request.Method != HttpMethod.Post)
            {
                return new HttpResponseMessage(HttpStatusCode.MethodNotAllowed);
            }

            if (!request.Headers.TryGetValues("Dropbox-API-Arg", out var args))
            {
                return new HttpResponseMessage(HttpStatusCode.BadRequest);
            }

            var argJson = JsonDocument.Parse(args.First());
            var fullPath = ReadPath(argJson);
            var normalizedDisplay = NormalizeDisplay(fullPath);
            var normalizedLower = NormalizeLower(normalizedDisplay);

            if (path.Equals("/2/files/upload", StringComparison.OrdinalIgnoreCase))
            {
                var content = request.Content == null
                    ? Array.Empty<byte>()
                    : await request.Content.ReadAsByteArrayAsync(cancellationToken);

                EnsureFolder(ParentPath(normalizedDisplay));
                var entry = UpsertFile(normalizedDisplay, content);
                return JsonResponse(ToMetadata(entry));
            }

            if (path.Equals("/2/files/download", StringComparison.OrdinalIgnoreCase))
            {
                if (!_entries.TryGetValue(normalizedLower, out var entry) || entry.IsFolder)
                {
                    return PathNotFoundError();
                }

                var response = new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new ByteArrayContent(entry.Content)
                };

                response.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/octet-stream");
                response.Headers.Add("Dropbox-API-Result", JsonSerializer.Serialize(ToMetadata(entry)));
                return response;
            }

            return new HttpResponseMessage(HttpStatusCode.NotFound);
        }

        private Entry EnsureFolder(string folderPathDisplay)
        {
            var normalized = NormalizeDisplay(folderPathDisplay);
            if (string.IsNullOrWhiteSpace(normalized) || normalized == "/")
            {
                return Entry.Folder("/", "/", "id:root");
            }

            var lower = NormalizeLower(normalized);
            if (_entries.TryGetValue(lower, out var existing) && existing.IsFolder)
            {
                return existing;
            }

            var parent = ParentPath(normalized);
            EnsureFolder(parent);

            var name = normalized.Split('/', StringSplitOptions.RemoveEmptyEntries).Last();
            var created = Entry.Folder(name, normalized, "id:folder:" + Guid.NewGuid().ToString("N"));
            _entries[lower] = created;
            return created;
        }

        private Entry UpsertFile(string filePathDisplay, byte[] content)
        {
            var normalizedDisplay = NormalizeDisplay(filePathDisplay);
            var lower = NormalizeLower(normalizedDisplay);
            var name = normalizedDisplay.Split('/', StringSplitOptions.RemoveEmptyEntries).Last();
            var entry = Entry.File(name, normalizedDisplay, "id:file:" + Guid.NewGuid().ToString("N"), content);
            _entries[lower] = entry;
            return entry;
        }

        private IEnumerable<Entry> ListChildren(string folderPathLower)
        {
            var normalized = NormalizeLower(NormalizeDisplay(folderPathLower));
            var prefix = normalized == "/" ? "/" : normalized.TrimEnd('/') + "/";
            return _entries.Values.Where(e => !e.PathLower.Equals(folderPathLower, StringComparison.OrdinalIgnoreCase)
                                              && string.Equals(ParentPath(e.PathLower), normalized, StringComparison.OrdinalIgnoreCase)
                                              && e.PathLower.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));
        }

        private Entry? DeleteRecursive(string normalizedLower)
        {
            if (!_entries.TryGetValue(normalizedLower, out var entry))
            {
                return null;
            }

            var keys = _entries.Keys
                .Where(k => k.Equals(normalizedLower, StringComparison.OrdinalIgnoreCase)
                            || k.StartsWith(normalizedLower.TrimEnd('/') + "/", StringComparison.OrdinalIgnoreCase))
                .ToList();

            foreach (var key in keys)
            {
                _entries.Remove(key);
            }

            return entry;
        }

        private static string ReadPath(JsonDocument? document)
        {
            if (document == null)
            {
                return string.Empty;
            }

            return document.RootElement.TryGetProperty("path", out var value) ? value.GetString() ?? string.Empty : string.Empty;
        }

        private static string NormalizeDisplay(string path)
        {
            var normalized = (path ?? string.Empty).Replace("\\", "/").Trim();
            if (string.IsNullOrWhiteSpace(normalized) || normalized == "/")
            {
                return "/";
            }

            if (!normalized.StartsWith('/'))
            {
                normalized = "/" + normalized;
            }

            return normalized.TrimEnd('/');
        }

        private static string NormalizeLower(string path)
        {
            var display = NormalizeDisplay(path);
            return display == "/" ? "/" : display.ToLowerInvariant();
        }

        private static string ParentPath(string path)
        {
            var normalized = NormalizeDisplay(path);
            if (normalized == "/")
            {
                return "/";
            }

            var lastSlash = normalized.LastIndexOf('/');
            return lastSlash <= 0 ? "/" : normalized[..lastSlash];
        }

        private static Dictionary<string, object?> ToMetadata(Entry entry)
        {
            if (entry.IsFolder)
            {
                return new Dictionary<string, object?>
                {
                    [".tag"] = "folder",
                    ["name"] = entry.Name,
                    ["path_lower"] = entry.PathLower,
                    ["path_display"] = entry.PathDisplay,
                    ["id"] = entry.Id
                };
            }

            return new Dictionary<string, object?>
            {
                [".tag"] = "file",
                ["name"] = entry.Name,
                ["path_lower"] = entry.PathLower,
                ["path_display"] = entry.PathDisplay,
                ["id"] = entry.Id,
                ["client_modified"] = entry.ClientModified.ToString("O"),
                ["server_modified"] = entry.ServerModified.ToString("O"),
                ["rev"] = entry.Rev,
                ["size"] = entry.Content.LongLength
            };
        }

        private static HttpResponseMessage JsonResponse(object payload, HttpStatusCode statusCode = HttpStatusCode.OK)
        {
            var response = new HttpResponseMessage(statusCode)
            {
                Content = new StringContent(JsonSerializer.Serialize(payload))
            };
            response.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");
            return response;
        }

        private static HttpResponseMessage PathNotFoundError()
        {
            return JsonResponse(new Dictionary<string, object?>
            {
                ["error_summary"] = "path/not_found/.",
                ["error"] = new Dictionary<string, object?>
                {
                    [".tag"] = "path",
                    ["path"] = new Dictionary<string, object?>
                    {
                        [".tag"] = "not_found"
                    }
                }
            }, HttpStatusCode.Conflict);
        }

        private static HttpResponseMessage PathLookupNotFoundError()
        {
            return JsonResponse(new Dictionary<string, object?>
            {
                ["error_summary"] = "path_lookup/not_found/",
                ["error"] = new Dictionary<string, object?>
                {
                    [".tag"] = "path_lookup",
                    ["path_lookup"] = new Dictionary<string, object?>
                    {
                        [".tag"] = "not_found"
                    }
                }
            }, HttpStatusCode.Conflict);
        }

        private sealed record Entry(string Name, string PathDisplay, string Id, bool IsFolder, byte[] Content, DateTime ClientModified, DateTime ServerModified, string Rev)
        {
            public string PathLower => PathDisplay == "/" ? "/" : PathDisplay.ToLowerInvariant();

            public static Entry Folder(string name, string pathDisplay, string id)
                => new(name, pathDisplay, id, true, Array.Empty<byte>(), DateTime.UtcNow, DateTime.UtcNow, "rev-folder");

            public static Entry File(string name, string pathDisplay, string id, byte[] content)
                => new(name, pathDisplay, id, false, content, DateTime.UtcNow, DateTime.UtcNow, "rev-file");
        }
    }
}
