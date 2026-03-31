using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace ManagedCode.Storage.Browser.Models;

internal sealed class BrowserStoredBlob
{
    [JsonPropertyName("key")]
    public string Key { get; set; } = string.Empty;

    [JsonPropertyName("payloadKey")]
    public string? PayloadKey { get; set; }

    [JsonPropertyName("container")]
    public string Container { get; set; } = string.Empty;

    [JsonPropertyName("fullName")]
    public string FullName { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("directory")]
    public string? Directory { get; set; }

    [JsonPropertyName("mimeType")]
    public string? MimeType { get; set; }

    [JsonPropertyName("metadata")]
    public Dictionary<string, string>? Metadata { get; set; }

    [JsonPropertyName("length")]
    public ulong Length { get; set; }

    [JsonPropertyName("chunkSizeBytes")]
    public int ChunkSizeBytes { get; set; }

    [JsonPropertyName("payloadStore")]
    public string PayloadStore { get; set; } = BrowserPayloadStores.Opfs;

    [JsonPropertyName("createdOn")]
    public DateTimeOffset CreatedOn { get; set; }

    [JsonPropertyName("lastModified")]
    public DateTimeOffset LastModified { get; set; }

    [JsonPropertyName("hasLegalHold")]
    public bool HasLegalHold { get; set; }
}
