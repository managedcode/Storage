using System;

namespace ManagedCode.Storage.CloudKit.Clients;

public sealed record CloudKitRecord(
    string RecordName,
    string RecordType,
    string Path,
    DateTimeOffset CreatedOn,
    DateTimeOffset LastModified,
    string? ContentType,
    ulong Size,
    Uri? DownloadUrl);

