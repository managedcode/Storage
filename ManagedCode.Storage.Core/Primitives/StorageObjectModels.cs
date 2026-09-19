using System;
using System.Collections.Generic;

namespace ManagedCode.Storage.Core.Primitives;

public sealed record StorageContainerInfo(string ETag, bool IsPrivate, IReadOnlyDictionary<string, string> Metadata);

public sealed record StorageObjectInfo(string Path, string ETag, long Length, string? ContentType,
    string? ContentEncoding, IReadOnlyDictionary<string, string> Metadata, DateTimeOffset? LastModified = null);

public sealed record StorageObjectPage(IReadOnlyList<StorageObjectInfo> Items, string? ContinuationToken);

public sealed record StorageReadOptions
{
    public string? IfMatch { get; init; }
    public long Offset { get; init; }
    public long? Length { get; init; }
}

public sealed record StorageWriteOptions
{
    public bool IfAbsent { get; init; }
    public string? IfMatch { get; init; }
    public string? ContentType { get; init; }
    public string? ContentEncoding { get; init; }
    public IReadOnlyDictionary<string, string>? Metadata { get; init; }
}

/// <summary>A provider-independent storage failure; cancellation is never translated.</summary>
public sealed class StorageOperationException(string message, int statusCode, Exception innerException, string? errorCode = null)
    : System.IO.IOException(message, innerException)
{
    public int StatusCode { get; } = statusCode;
    public string? ErrorCode { get; } = errorCode;
    public bool IsNotFound => StatusCode == 404;
    public bool IsConflict => StatusCode is 409 or 412;
}

public static class ObjectStorageCapabilities
{
    public static IObjectStorage RequireObjectStorage(this IStorage storage) =>
        storage as IObjectStorage ?? throw new NotSupportedException("The storage provider does not support atomic object operations.");

    public static IMultipartObjectStorage RequireMultipartStorage(this IStorage storage) =>
        storage as IMultipartObjectStorage ?? throw new NotSupportedException("The storage provider does not support resumable multipart operations.");
}
