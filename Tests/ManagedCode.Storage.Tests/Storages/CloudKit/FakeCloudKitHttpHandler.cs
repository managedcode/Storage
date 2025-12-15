using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace ManagedCode.Storage.Tests.Storages.CloudKit;

internal sealed class FakeCloudKitHttpHandler : HttpMessageHandler
{
    private const string CloudKitHost = "api.apple-cloudkit.com";
    private const string AssetsHost = "assets.example";

    private readonly Dictionary<string, StoredRecord> _records = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, byte[]> _uploads = new(StringComparer.OrdinalIgnoreCase);
    private string? _expectedWebAuthToken;
    private int _webAuthTokenCounter;

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var host = request.RequestUri?.Host ?? string.Empty;
        var path = request.RequestUri?.AbsolutePath ?? string.Empty;

        if (host.Equals(CloudKitHost, StringComparison.OrdinalIgnoreCase))
        {
            return await HandleCloudKitAsync(request, path, cancellationToken);
        }

        if (host.Equals(AssetsHost, StringComparison.OrdinalIgnoreCase))
        {
            return await HandleAssetsAsync(request, path, cancellationToken);
        }

        return new HttpResponseMessage(HttpStatusCode.NotFound);
    }

    private async Task<HttpResponseMessage> HandleCloudKitAsync(HttpRequestMessage request, string path, CancellationToken cancellationToken)
    {
        if (request.Method != HttpMethod.Post)
        {
            return new HttpResponseMessage(HttpStatusCode.MethodNotAllowed);
        }

        var query = ParseQuery(request.RequestUri?.Query);
        var (rotatedWebAuthToken, webAuthError) = ValidateAndRotateWebAuthToken(query);
        if (webAuthError != null)
        {
            return webAuthError;
        }

        if (path.EndsWith("/assets/upload", StringComparison.OrdinalIgnoreCase))
        {
            var body = await request.Content!.ReadAsStringAsync(cancellationToken);
            using var doc = JsonDocument.Parse(body);
            var token = doc.RootElement.GetProperty("tokens").EnumerateArray().First();
            var recordName = token.GetProperty("recordName").GetString() ?? string.Empty;
            var fieldName = token.GetProperty("fieldName").GetString() ?? "file";

            return JsonResponseWithToken(new Dictionary<string, object?>
            {
                ["tokens"] = new[]
                {
                    new
                    {
                        recordName,
                        fieldName,
                        url = $"https://{AssetsHost}/upload/{recordName}"
                    }
                }
            }, rotatedWebAuthToken);
        }

        if (path.EndsWith("/records/modify", StringComparison.OrdinalIgnoreCase))
        {
            var body = await request.Content!.ReadAsStringAsync(cancellationToken);
            using var doc = JsonDocument.Parse(body);
            var operation = doc.RootElement.GetProperty("operations").EnumerateArray().First();
            var type = operation.GetProperty("operationType").GetString();
            var record = operation.GetProperty("record");
            var recordName = record.GetProperty("recordName").GetString() ?? string.Empty;

            if (string.Equals(type, "forceDelete", StringComparison.OrdinalIgnoreCase))
            {
                if (_records.Remove(recordName))
                {
                    return JsonResponseWithToken(new Dictionary<string, object?>(), rotatedWebAuthToken);
                }

                return JsonResponseWithToken(new Dictionary<string, object?>
                {
                    ["errors"] = new[]
                    {
                        new
                        {
                            recordName,
                            serverErrorCode = "NOT_FOUND"
                        }
                    }
                }, rotatedWebAuthToken);
            }

            if (!string.Equals(type, "forceUpdate", StringComparison.OrdinalIgnoreCase))
            {
                return new HttpResponseMessage(HttpStatusCode.BadRequest);
            }

            var recordType = record.GetProperty("recordType").GetString() ?? "MCStorageFile";
            var fields = record.GetProperty("fields");
            var internalPath = fields.GetProperty("path").GetProperty("value").GetString() ?? string.Empty;
            var contentType = fields.GetProperty("contentType").GetProperty("value").GetString() ?? "application/octet-stream";

            var content = _uploads.TryGetValue(recordName, out var bytes) ? bytes : Array.Empty<byte>();
            var now = DateTimeOffset.UtcNow;

            var stored = new StoredRecord(recordName, recordType, internalPath, contentType, content, now, now);
            _records[recordName] = stored;

            return JsonResponseWithToken(new Dictionary<string, object?>
            {
                ["records"] = new[]
                {
                    ToRecordResponse(stored)
                }
            }, rotatedWebAuthToken);
        }

        if (path.EndsWith("/records/lookup", StringComparison.OrdinalIgnoreCase))
        {
            var body = await request.Content!.ReadAsStringAsync(cancellationToken);
            using var doc = JsonDocument.Parse(body);
            var recordName = doc.RootElement.GetProperty("records").EnumerateArray().First().GetProperty("recordName").GetString() ?? string.Empty;

            if (!_records.TryGetValue(recordName, out var stored))
            {
                return JsonResponseWithToken(new Dictionary<string, object?>
                {
                    ["errors"] = new[]
                    {
                        new
                        {
                            recordName,
                            serverErrorCode = "NOT_FOUND"
                        }
                    }
                }, rotatedWebAuthToken);
            }

            return JsonResponseWithToken(new Dictionary<string, object?>
            {
                ["records"] = new[]
                {
                    ToRecordResponse(stored)
                }
            }, rotatedWebAuthToken);
        }

        if (path.EndsWith("/records/query", StringComparison.OrdinalIgnoreCase))
        {
            var body = await request.Content!.ReadAsStringAsync(cancellationToken);
            using var doc = JsonDocument.Parse(body);
            var prefix = doc.RootElement.GetProperty("query")
                .GetProperty("filterBy")
                .EnumerateArray()
                .First()
                .GetProperty("fieldValue")
                .GetProperty("value")
                .GetString() ?? string.Empty;

            var results = _records.Values
                .Where(r => r.Path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                .Select(ToRecordResponse)
                .ToList();

            return JsonResponseWithToken(new Dictionary<string, object?>
            {
                ["records"] = results
            }, rotatedWebAuthToken);
        }

        return new HttpResponseMessage(HttpStatusCode.NotFound)
        {
            Content = new StringContent($"Unhandled CloudKit request: {request.Method} {request.RequestUri}")
        };
    }

    private async Task<HttpResponseMessage> HandleAssetsAsync(HttpRequestMessage request, string path, CancellationToken cancellationToken)
    {
        if (path.StartsWith("/upload/", StringComparison.OrdinalIgnoreCase))
        {
            var recordName = path["/upload/".Length..];
            var bytes = request.Content == null ? Array.Empty<byte>() : await request.Content.ReadAsByteArrayAsync(cancellationToken);
            _uploads[recordName] = bytes;

            return JsonResponse(new
            {
                singleFile = new
                {
                    receipt = "receipt-" + recordName,
                    size = bytes.LongLength,
                    fileChecksum = Convert.ToBase64String(Encoding.UTF8.GetBytes("checksum"))
                }
            });
        }

        if (path.StartsWith("/download/", StringComparison.OrdinalIgnoreCase))
        {
            var recordName = path["/download/".Length..];
            if (!_records.TryGetValue(recordName, out var record))
            {
                return new HttpResponseMessage(HttpStatusCode.NotFound);
            }

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new ByteArrayContent(record.Content)
            };
        }

        return new HttpResponseMessage(HttpStatusCode.NotFound);
    }

    private static object ToRecordResponse(StoredRecord record)
    {
        var timestamp = record.LastModified.ToUnixTimeMilliseconds();
        return new
        {
            recordName = record.RecordName,
            recordType = record.RecordType,
            fields = new Dictionary<string, object?>
            {
                ["path"] = new Dictionary<string, object?> { ["value"] = record.Path },
                ["contentType"] = new Dictionary<string, object?> { ["value"] = record.ContentType },
                ["file"] = new Dictionary<string, object?>
                {
                    ["value"] = new Dictionary<string, object?>
                    {
                        ["downloadURL"] = $"https://{AssetsHost}/download/{record.RecordName}",
                        ["size"] = record.Content.LongLength
                    }
                }
            },
            created = new
            {
                timestamp
            },
            modified = new
            {
                timestamp
            }
        };
    }

    private static HttpResponseMessage JsonResponse(object payload, HttpStatusCode statusCode = HttpStatusCode.OK)
    {
        var json = JsonSerializer.Serialize(payload, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
        return new HttpResponseMessage(statusCode)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };
    }

    private static HttpResponseMessage JsonResponseWithToken(Dictionary<string, object?> payload, string? webAuthToken, HttpStatusCode statusCode = HttpStatusCode.OK)
    {
        if (!string.IsNullOrWhiteSpace(webAuthToken))
        {
            payload["ckWebAuthToken"] = webAuthToken;
        }

        return JsonResponse(payload, statusCode);
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

    private (string? RotatedToken, HttpResponseMessage? ErrorResponse) ValidateAndRotateWebAuthToken(Dictionary<string, string> query)
    {
        if (!query.TryGetValue("ckWebAuthToken", out var token) || string.IsNullOrWhiteSpace(token))
        {
            return (null, null);
        }

        if (_expectedWebAuthToken == null)
        {
            _expectedWebAuthToken = token;
        }
        else if (!string.Equals(_expectedWebAuthToken, token, StringComparison.Ordinal))
        {
            return (null, JsonResponse(new
            {
                uuid = Guid.NewGuid().ToString("N"),
                serverErrorCode = "AUTHENTICATION_REQUIRED",
                reason = "Invalid ckWebAuthToken."
            }, HttpStatusCode.Unauthorized));
        }

        var rotated = "web-token-" + Interlocked.Increment(ref _webAuthTokenCounter);
        _expectedWebAuthToken = rotated;
        return (rotated, null);
    }

    private sealed record StoredRecord(
        string RecordName,
        string RecordType,
        string Path,
        string ContentType,
        byte[] Content,
        DateTimeOffset CreatedOn,
        DateTimeOffset LastModified);
}
