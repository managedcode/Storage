using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace ManagedCode.Storage.Core.Objects;

/// <summary>Optional atomic object operations. Providers must enforce conditions server-side.</summary>
public interface IObjectStorage
{
    Uri ContainerUri { get; }
    Task<StorageContainerInfo> GetContainerInfoAsync(CancellationToken cancellationToken = default);
    Task CreatePrivateContainerAsync(IReadOnlyDictionary<string, string>? metadata = null, CancellationToken cancellationToken = default);
    Task SetContainerMetadataAsync(IReadOnlyDictionary<string, string> metadata, CancellationToken cancellationToken = default);
    Task<bool> DeleteContainerIfExistsAsync(CancellationToken cancellationToken = default);
    Task<StorageObjectInfo> GetObjectInfoAsync(string path, CancellationToken cancellationToken = default);
    Task<bool> ObjectExistsAsync(string path, CancellationToken cancellationToken = default);
    Task<Stream> OpenObjectReadAsync(string path, StorageReadOptions? options = null, CancellationToken cancellationToken = default);
    Task<StorageObjectInfo> WriteObjectAsync(string path, Stream content, StorageWriteOptions? options = null, CancellationToken cancellationToken = default);
    Task SetObjectMetadataAsync(string path, IReadOnlyDictionary<string, string> metadata, string? ifMatch = null, CancellationToken cancellationToken = default);
    Task<bool> DeleteObjectIfExistsAsync(string path, bool includeSnapshots = false, CancellationToken cancellationToken = default);
    Task<StorageObjectPage> ListObjectsAsync(string? prefix = null, string? continuationToken = null, int pageSize = 100, CancellationToken cancellationToken = default);
}

/// <summary>Optional resumable multipart writes. Staged parts stay invisible until committed.</summary>
public interface IMultipartObjectStorage : IObjectStorage
{
    Task StagePartAsync(string path, string partId, Stream content, CancellationToken cancellationToken = default);
    Task<StorageObjectInfo> CommitPartsAsync(string path, IReadOnlyList<string> partIds, StorageWriteOptions? options = null, CancellationToken cancellationToken = default);
}
