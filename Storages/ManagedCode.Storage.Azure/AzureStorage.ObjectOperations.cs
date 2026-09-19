using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using ManagedCode.Storage.Core.Primitives;

namespace ManagedCode.Storage.Azure;

public partial class AzureStorage
{
    private AzureObjectOperations ObjectOperations => new(StorageClient);
    public Uri ContainerUri => ObjectOperations.ContainerUri;

    public Task<StorageContainerInfo> GetContainerInfoAsync(CancellationToken cancellationToken = default) =>
        ObjectOperations.GetContainerInfoAsync(cancellationToken);

    public Task CreatePrivateContainerAsync(IReadOnlyDictionary<string, string>? metadata = null, CancellationToken cancellationToken = default) =>
        ObjectOperations.CreatePrivateContainerAsync(metadata, cancellationToken);

    public Task SetContainerMetadataAsync(IReadOnlyDictionary<string, string> metadata, CancellationToken cancellationToken = default) =>
        ObjectOperations.SetContainerMetadataAsync(metadata, cancellationToken);

    public Task<bool> DeleteContainerIfExistsAsync(CancellationToken cancellationToken = default) =>
        ObjectOperations.DeleteContainerIfExistsAsync(cancellationToken);

    public Task<StorageObjectInfo> GetObjectInfoAsync(string path, CancellationToken cancellationToken = default) =>
        ObjectOperations.GetObjectInfoAsync(path, cancellationToken);

    public Task<bool> ObjectExistsAsync(string path, CancellationToken cancellationToken = default) =>
        ObjectOperations.ObjectExistsAsync(path, cancellationToken);

    public Task<Stream> OpenObjectReadAsync(string path, StorageReadOptions? options = null, CancellationToken cancellationToken = default) =>
        ObjectOperations.OpenObjectReadAsync(path, options, cancellationToken);

    public Task<StorageObjectInfo> WriteObjectAsync(string path, Stream content, StorageWriteOptions? options = null, CancellationToken cancellationToken = default) =>
        ObjectOperations.WriteObjectAsync(path, content, options, cancellationToken);

    public Task SetObjectMetadataAsync(string path, IReadOnlyDictionary<string, string> metadata, string? ifMatch = null, CancellationToken cancellationToken = default) =>
        ObjectOperations.SetObjectMetadataAsync(path, metadata, ifMatch, cancellationToken);

    public Task<bool> DeleteObjectIfExistsAsync(string path, bool includeSnapshots = false, CancellationToken cancellationToken = default) =>
        ObjectOperations.DeleteObjectIfExistsAsync(path, includeSnapshots, cancellationToken);

    public Task<StorageObjectPage> ListObjectsAsync(string? prefix = null, string? continuationToken = null, int pageSize = 100, CancellationToken cancellationToken = default) =>
        ObjectOperations.ListObjectsAsync(prefix, continuationToken, pageSize, cancellationToken);

    public Task StagePartAsync(string path, string partId, Stream content, CancellationToken cancellationToken = default) =>
        ObjectOperations.StagePartAsync(path, partId, content, cancellationToken);

    public Task<StorageObjectInfo> CommitPartsAsync(string path, IReadOnlyList<string> partIds, StorageWriteOptions? options = null, CancellationToken cancellationToken = default) =>
        ObjectOperations.CommitPartsAsync(path, partIds, options, cancellationToken);
}
