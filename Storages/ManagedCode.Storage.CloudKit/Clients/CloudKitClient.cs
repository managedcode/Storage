using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using ManagedCode.Storage.CloudKit.Options;

namespace ManagedCode.Storage.CloudKit.Clients;

public sealed class CloudKitClient : ICloudKitClient, IDisposable
{
    private static readonly Uri BaseUri = new("https://api.apple-cloudkit.com", UriKind.Absolute);

    private readonly CloudKitStorageOptions _options;
    private readonly HttpClient _httpClient;
    private readonly bool _ownsHttpClient;
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
    };

    private readonly ECDsa? _signingKey;

    public CloudKitClient(CloudKitStorageOptions options, HttpClient? httpClient = null)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _httpClient = httpClient ?? new HttpClient();
        _ownsHttpClient = httpClient == null;

        if (!string.IsNullOrWhiteSpace(_options.ServerToServerPrivateKeyPem))
        {
            _signingKey = ECDsa.Create();
            _signingKey.ImportFromPem(_options.ServerToServerPrivateKeyPem);
        }
    }

    public async Task<CloudKitRecord> UploadAsync(string recordName, string internalPath, Stream content, string contentType, CancellationToken cancellationToken)
    {
        var uploadUrl = await GetAssetUploadUrlAsync(recordName, cancellationToken);
        var assetValue = await UploadAssetAsync(uploadUrl, content, contentType, cancellationToken);
        var record = await UpsertRecordAsync(recordName, internalPath, contentType, assetValue, cancellationToken);
        return record;
    }

    public async Task<Stream> DownloadAsync(string recordName, CancellationToken cancellationToken)
    {
        var record = await GetRecordAsync(recordName, cancellationToken)
                     ?? throw new FileNotFoundException($"CloudKit record '{recordName}' not found.");

        if (record.DownloadUrl == null)
        {
            throw new InvalidOperationException("CloudKit record does not include an asset download URL.");
        }

        return await DownloadFromUrlAsync(record.DownloadUrl, cancellationToken);
    }

    public async Task<bool> DeleteAsync(string recordName, CancellationToken cancellationToken)
    {
        var payload = new Dictionary<string, object?>
        {
            ["operations"] = new object[]
            {
                new Dictionary<string, object?>
                {
                    ["operationType"] = "forceDelete",
                    ["record"] = new Dictionary<string, object?>
                    {
                        ["recordName"] = recordName
                    }
                }
            }
        };

        var document = await SendCloudKitAsync("records/modify", payload, cancellationToken);
        if (TryGetRecordErrorCode(document.RootElement, out var errorCode))
        {
            if (errorCode == "NOT_FOUND")
            {
                return false;
            }

            throw new InvalidOperationException($"CloudKit delete failed with error code '{errorCode}'.");
        }

        return true;
    }

    public async Task<bool> ExistsAsync(string recordName, CancellationToken cancellationToken)
    {
        return await GetRecordAsync(recordName, cancellationToken) != null;
    }

    public async Task<CloudKitRecord?> GetRecordAsync(string recordName, CancellationToken cancellationToken)
    {
        var payload = new Dictionary<string, object?>
        {
            ["records"] = new object[]
            {
                new Dictionary<string, object?>
                {
                    ["recordName"] = recordName
                }
            },
            ["desiredKeys"] = new[] { _options.PathFieldName, _options.ContentTypeFieldName, _options.AssetFieldName }
        };

        var document = await SendCloudKitAsync("records/lookup", payload, cancellationToken);
        if (TryGetRecordErrorCode(document.RootElement, out var errorCode))
        {
            if (errorCode == "NOT_FOUND")
            {
                return null;
            }

            throw new InvalidOperationException($"CloudKit lookup failed with error code '{errorCode}'.");
        }

        if (!document.RootElement.TryGetProperty("records", out var records) || records.ValueKind != JsonValueKind.Array)
        {
            return null;
        }

        var first = records.EnumerateArray().FirstOrDefault();
        return first.ValueKind == JsonValueKind.Object ? ParseRecord(first) : null;
    }

    public async IAsyncEnumerable<CloudKitRecord> QueryByPathPrefixAsync(string pathPrefix, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var marker = (string?)null;
        do
        {
            cancellationToken.ThrowIfCancellationRequested();

            var payload = new Dictionary<string, object?>
            {
                ["query"] = new Dictionary<string, object?>
                {
                    ["recordType"] = _options.RecordType,
                    ["filterBy"] = new object[]
                    {
                        new Dictionary<string, object?>
                        {
                            ["fieldName"] = _options.PathFieldName,
                            ["comparator"] = "BEGINS_WITH",
                            ["fieldValue"] = new Dictionary<string, object?>
                            {
                                ["value"] = pathPrefix
                            }
                        }
                    }
                },
                ["desiredKeys"] = new[] { _options.PathFieldName, _options.ContentTypeFieldName, _options.AssetFieldName },
                ["resultsLimit"] = 200
            };

            if (!string.IsNullOrWhiteSpace(marker))
            {
                payload["continuationMarker"] = marker;
            }

            var document = await SendCloudKitAsync("records/query", payload, cancellationToken);
            if (document.RootElement.TryGetProperty("records", out var records) && records.ValueKind == JsonValueKind.Array)
            {
                foreach (var record in records.EnumerateArray())
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    if (record.ValueKind == JsonValueKind.Object)
                    {
                        yield return ParseRecord(record);
                    }
                }
            }

            marker = document.RootElement.TryGetProperty("continuationMarker", out var markerElement) && markerElement.ValueKind == JsonValueKind.String
                ? markerElement.GetString()
                : null;
        } while (!string.IsNullOrWhiteSpace(marker));
    }

    private async Task<Uri> GetAssetUploadUrlAsync(string recordName, CancellationToken cancellationToken)
    {
        var payload = new Dictionary<string, object?>
        {
            ["tokens"] = new object[]
            {
                new Dictionary<string, object?>
                {
                    ["recordType"] = _options.RecordType,
                    ["recordName"] = recordName,
                    ["fieldName"] = _options.AssetFieldName
                }
            }
        };

        var document = await SendCloudKitAsync("assets/upload", payload, cancellationToken);
        if (!document.RootElement.TryGetProperty("tokens", out var tokens) || tokens.ValueKind != JsonValueKind.Array)
        {
            throw new InvalidOperationException("CloudKit assets/upload response does not include tokens.");
        }

        var token = tokens.EnumerateArray().FirstOrDefault();
        if (token.ValueKind != JsonValueKind.Object || !token.TryGetProperty("url", out var urlElement) || urlElement.ValueKind != JsonValueKind.String)
        {
            throw new InvalidOperationException("CloudKit assets/upload response does not include an upload URL.");
        }

        return new Uri(urlElement.GetString()!, UriKind.Absolute);
    }

    private async Task<JsonElement> UploadAssetAsync(Uri uploadUrl, Stream content, string contentType, CancellationToken cancellationToken)
    {
        if (content.CanSeek)
        {
            content.Position = 0;
        }

        using var request = new HttpRequestMessage(HttpMethod.Post, uploadUrl)
        {
            Content = new StreamContent(content)
        };

        request.Content.Headers.ContentType = new MediaTypeHeaderValue(string.IsNullOrWhiteSpace(contentType) ? "application/octet-stream" : contentType);

        using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseContentRead, cancellationToken);
        var json = await response.Content.ReadAsByteArrayAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException($"CloudKit asset upload failed with status {(int)response.StatusCode}.", null, response.StatusCode);
        }

        using var document = JsonDocument.Parse(json);
        if (!document.RootElement.TryGetProperty("singleFile", out var singleFile) || singleFile.ValueKind != JsonValueKind.Object)
        {
            throw new InvalidOperationException("CloudKit asset upload response does not include 'singleFile'.");
        }

        return singleFile.Clone();
    }

    private async Task<CloudKitRecord> UpsertRecordAsync(string recordName, string internalPath, string contentType, JsonElement assetValue, CancellationToken cancellationToken)
    {
        var fields = new Dictionary<string, object?>
        {
            [_options.PathFieldName] = new Dictionary<string, object?> { ["value"] = internalPath },
            [_options.ContentTypeFieldName] = new Dictionary<string, object?> { ["value"] = contentType },
            [_options.AssetFieldName] = new Dictionary<string, object?> { ["value"] = assetValue }
        };

        var payload = new Dictionary<string, object?>
        {
            ["operations"] = new object[]
            {
                new Dictionary<string, object?>
                {
                    ["operationType"] = "forceUpdate",
                    ["record"] = new Dictionary<string, object?>
                    {
                        ["recordType"] = _options.RecordType,
                        ["recordName"] = recordName,
                        ["fields"] = fields
                    }
                }
            }
        };

        var document = await SendCloudKitAsync("records/modify", payload, cancellationToken);
        if (TryGetRecordErrorCode(document.RootElement, out var errorCode))
        {
            throw new InvalidOperationException($"CloudKit modify failed with error code '{errorCode}'.");
        }

        if (!document.RootElement.TryGetProperty("records", out var records) || records.ValueKind != JsonValueKind.Array)
        {
            throw new InvalidOperationException("CloudKit modify response does not include records.");
        }

        var first = records.EnumerateArray().FirstOrDefault();
        if (first.ValueKind != JsonValueKind.Object)
        {
            throw new InvalidOperationException("CloudKit modify response did not return a record.");
        }

        return ParseRecord(first);
    }

    private async Task<JsonDocument> SendCloudKitAsync(string operation, object payload, CancellationToken cancellationToken)
    {
        var subpath = BuildSubpath(operation);
        var uri = BuildUri(subpath);
        var body = JsonSerializer.SerializeToUtf8Bytes(payload, _jsonOptions);

        using var request = new HttpRequestMessage(HttpMethod.Post, uri);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        request.Content = new ByteArrayContent(body);
        request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

        ApplyAuthentication(request, subpath, body);

        using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseContentRead, cancellationToken);
        var json = await response.Content.ReadAsByteArrayAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException($"CloudKit request failed: {(int)response.StatusCode} {response.ReasonPhrase}", null, response.StatusCode);
        }

        return JsonDocument.Parse(json);
    }

    private async Task<Stream> DownloadFromUrlAsync(Uri downloadUrl, CancellationToken cancellationToken)
    {
        var response = await _httpClient.GetAsync(downloadUrl, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            response.Dispose();
            throw new HttpRequestException($"CloudKit asset download failed: {(int)response.StatusCode} {response.ReasonPhrase}", null, response.StatusCode);
        }

        var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        return new ResponseDisposingStream(stream, response);
    }

    private string BuildSubpath(string operation)
    {
        var environment = _options.Environment == CloudKitEnvironment.Production ? "production" : "development";
        var database = _options.Database.ToString().ToLowerInvariant();
        return $"/database/1/{_options.ContainerId}/{environment}/{database}/{operation}";
    }

    private Uri BuildUri(string subpath)
    {
        var builder = new UriBuilder(BaseUri)
        {
            Path = subpath
        };

        var queryParts = new List<string>();
        if (!string.IsNullOrWhiteSpace(_options.ApiToken))
        {
            queryParts.Add("ckAPIToken=" + Uri.EscapeDataString(_options.ApiToken));
        }

        if (!string.IsNullOrWhiteSpace(_options.WebAuthToken))
        {
            queryParts.Add("ckWebAuthToken=" + Uri.EscapeDataString(_options.WebAuthToken));
        }

        builder.Query = string.Join('&', queryParts);
        return builder.Uri;
    }

    private void ApplyAuthentication(HttpRequestMessage request, string subpath, byte[] body)
    {
        if (!string.IsNullOrWhiteSpace(_options.ApiToken))
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(_options.ServerToServerKeyId) || _signingKey == null)
        {
            return;
        }

        var date = DateTimeOffset.UtcNow.ToString("yyyy-MM-dd'T'HH:mm:ss'Z'");
        var bodyHash = Convert.ToBase64String(SHA256.HashData(body));
        var signatureData = $"{date}:{bodyHash}:{subpath}";
        var signatureBytes = _signingKey.SignData(Encoding.UTF8.GetBytes(signatureData), HashAlgorithmName.SHA256);
        var signature = Convert.ToBase64String(signatureBytes);

        request.Headers.TryAddWithoutValidation("X-Apple-CloudKit-Request-KeyID", _options.ServerToServerKeyId);
        request.Headers.TryAddWithoutValidation("X-Apple-CloudKit-Request-ISO8601Date", date);
        request.Headers.TryAddWithoutValidation("X-Apple-CloudKit-Request-SignatureV1", signature);
    }

    private CloudKitRecord ParseRecord(JsonElement record)
    {
        var recordName = record.TryGetProperty("recordName", out var nameElement) && nameElement.ValueKind == JsonValueKind.String
            ? nameElement.GetString() ?? string.Empty
            : string.Empty;

        var recordType = record.TryGetProperty("recordType", out var typeElement) && typeElement.ValueKind == JsonValueKind.String
            ? typeElement.GetString() ?? _options.RecordType
            : _options.RecordType;

        var createdOn = ReadTimestamp(record, "created");
        var lastModified = ReadTimestamp(record, "modified");

        var fields = record.TryGetProperty("fields", out var fieldsElement) && fieldsElement.ValueKind == JsonValueKind.Object
            ? fieldsElement
            : default;

        var path = ReadStringField(fields, _options.PathFieldName) ?? string.Empty;
        var contentType = ReadStringField(fields, _options.ContentTypeFieldName);

        var (downloadUrl, size) = ReadAsset(fields, _options.AssetFieldName);

        return new CloudKitRecord(
            RecordName: recordName,
            RecordType: recordType,
            Path: path,
            CreatedOn: createdOn,
            LastModified: lastModified,
            ContentType: contentType,
            Size: size,
            DownloadUrl: downloadUrl);
    }

    private static DateTimeOffset ReadTimestamp(JsonElement record, string propertyName)
    {
        if (!record.TryGetProperty(propertyName, out var element) || element.ValueKind != JsonValueKind.Object)
        {
            return DateTimeOffset.UtcNow;
        }

        if (!element.TryGetProperty("timestamp", out var timestamp) || (timestamp.ValueKind != JsonValueKind.Number && timestamp.ValueKind != JsonValueKind.String))
        {
            return DateTimeOffset.UtcNow;
        }

        if (timestamp.ValueKind == JsonValueKind.Number && timestamp.TryGetInt64(out var ms))
        {
            return DateTimeOffset.FromUnixTimeMilliseconds(ms);
        }

        if (timestamp.ValueKind == JsonValueKind.String && long.TryParse(timestamp.GetString(), out ms))
        {
            return DateTimeOffset.FromUnixTimeMilliseconds(ms);
        }

        return DateTimeOffset.UtcNow;
    }

    private static string? ReadStringField(JsonElement fields, string fieldName)
    {
        if (fields.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        if (!fields.TryGetProperty(fieldName, out var field) || field.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        if (!field.TryGetProperty("value", out var value) || value.ValueKind != JsonValueKind.String)
        {
            return null;
        }

        return value.GetString();
    }

    private static (Uri? DownloadUrl, ulong Size) ReadAsset(JsonElement fields, string fieldName)
    {
        if (fields.ValueKind != JsonValueKind.Object)
        {
            return (null, 0);
        }

        if (!fields.TryGetProperty(fieldName, out var field) || field.ValueKind != JsonValueKind.Object)
        {
            return (null, 0);
        }

        if (!field.TryGetProperty("value", out var value) || value.ValueKind != JsonValueKind.Object)
        {
            return (null, 0);
        }

        Uri? downloadUrl = null;
        if (value.TryGetProperty("downloadURL", out var urlElement) && urlElement.ValueKind == JsonValueKind.String)
        {
            var raw = urlElement.GetString();
            if (!string.IsNullOrWhiteSpace(raw) && Uri.TryCreate(raw, UriKind.Absolute, out var parsed))
            {
                downloadUrl = parsed;
            }
        }

        ulong size = 0;
        if (value.TryGetProperty("size", out var sizeElement))
        {
            if (sizeElement.ValueKind == JsonValueKind.Number && sizeElement.TryGetUInt64(out var parsed))
            {
                size = parsed;
            }
            else if (sizeElement.ValueKind == JsonValueKind.String && ulong.TryParse(sizeElement.GetString(), out parsed))
            {
                size = parsed;
            }
        }

        return (downloadUrl, size);
    }

    private static bool TryGetRecordErrorCode(JsonElement response, out string errorCode)
    {
        errorCode = string.Empty;

        if (!response.TryGetProperty("errors", out var errors) || errors.ValueKind != JsonValueKind.Array)
        {
            return false;
        }

        var first = errors.EnumerateArray().FirstOrDefault();
        if (first.ValueKind != JsonValueKind.Object)
        {
            return false;
        }

        if (first.TryGetProperty("serverErrorCode", out var codeElement) && codeElement.ValueKind == JsonValueKind.String)
        {
            errorCode = codeElement.GetString() ?? string.Empty;
            return !string.IsNullOrWhiteSpace(errorCode);
        }

        return false;
    }

    public void Dispose()
    {
        _signingKey?.Dispose();
        if (_ownsHttpClient)
        {
            _httpClient.Dispose();
        }
    }

    private sealed class ResponseDisposingStream : Stream
    {
        private readonly Stream _inner;
        private readonly HttpResponseMessage _response;

        public ResponseDisposingStream(Stream inner, HttpResponseMessage response)
        {
            _inner = inner;
            _response = response;
        }

        public override bool CanRead => _inner.CanRead;
        public override bool CanSeek => _inner.CanSeek;
        public override bool CanWrite => _inner.CanWrite;
        public override long Length => _inner.Length;

        public override long Position
        {
            get => _inner.Position;
            set => _inner.Position = value;
        }

        public override void Flush() => _inner.Flush();

        public override int Read(byte[] buffer, int offset, int count) => _inner.Read(buffer, offset, count);

        public override long Seek(long offset, SeekOrigin origin) => _inner.Seek(offset, origin);

        public override void SetLength(long value) => _inner.SetLength(value);

        public override void Write(byte[] buffer, int offset, int count) => _inner.Write(buffer, offset, count);

        public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            return await _inner.ReadAsync(buffer, cancellationToken);
        }

        public override async Task CopyToAsync(Stream destination, int bufferSize, CancellationToken cancellationToken)
        {
            await _inner.CopyToAsync(destination, bufferSize, cancellationToken);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _inner.Dispose();
                _response.Dispose();
            }

            base.Dispose(disposing);
        }

        public override async ValueTask DisposeAsync()
        {
            await _inner.DisposeAsync();
            _response.Dispose();
            await base.DisposeAsync();
        }
    }
}
