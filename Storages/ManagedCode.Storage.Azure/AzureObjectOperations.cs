using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Blobs.Specialized;
using ManagedCode.Storage.Core.Primitives;

namespace ManagedCode.Storage.Azure;

internal sealed class AzureObjectOperations(BlobContainerClient container) : IMultipartObjectStorage
{
    public Uri ContainerUri => container.Uri;

    public Task<StorageContainerInfo> GetContainerInfoAsync(CancellationToken cancellationToken = default) => ExecuteAsync(async () =>
    {
        var value = (await container.GetPropertiesAsync(cancellationToken: cancellationToken)).Value;
        return new StorageContainerInfo(value.ETag.ToString(), value.PublicAccess == PublicAccessType.None,
            new Dictionary<string, string>(value.Metadata));
    });

    public Task CreatePrivateContainerAsync(IReadOnlyDictionary<string, string>? metadata = null, CancellationToken cancellationToken = default) => ExecuteAsync(async () =>
    {
        await container.CreateIfNotExistsAsync(PublicAccessType.None, Copy(metadata), cancellationToken: cancellationToken);
        return true;
    });

    public Task SetContainerMetadataAsync(IReadOnlyDictionary<string, string> metadata, CancellationToken cancellationToken = default) => ExecuteAsync(async () =>
    {
        await container.SetMetadataAsync(Copy(metadata), cancellationToken: cancellationToken);
        return true;
    });

    public Task<bool> DeleteContainerIfExistsAsync(CancellationToken cancellationToken = default) => ExecuteAsync(async () =>
        (await container.DeleteIfExistsAsync(cancellationToken: cancellationToken)).Value);

    public Task<StorageObjectInfo> GetObjectInfoAsync(string path, CancellationToken cancellationToken = default) => ExecuteAsync(async () =>
        Info(path, (await container.GetBlobClient(path).GetPropertiesAsync(cancellationToken: cancellationToken)).Value));

    public Task<bool> ObjectExistsAsync(string path, CancellationToken cancellationToken = default) => ExecuteAsync(async () =>
        (await container.GetBlobClient(path).ExistsAsync(cancellationToken)).Value);

    public Task<Stream> OpenObjectReadAsync(string path, StorageReadOptions? options = null, CancellationToken cancellationToken = default) => ExecuteAsync(async () =>
    {
        options ??= new StorageReadOptions();
        ArgumentOutOfRangeException.ThrowIfNegative(options.Offset);
        if (options.Length is <= 0) throw new ArgumentOutOfRangeException(nameof(options));
        var blob = container.GetBlobClient(path);
        var etag = options.IfMatch ?? (await blob.GetPropertiesAsync(cancellationToken: cancellationToken)).Value.ETag.ToString();
        if (options.Length is null)
        {
            return await blob.OpenReadAsync(new BlobOpenReadOptions(false)
            {
                Position = options.Offset,
                Conditions = new BlobRequestConditions { IfMatch = new ETag(etag) }
            }, cancellationToken);
        }
        var download = await blob.DownloadStreamingAsync(new BlobDownloadOptions
        {
            Range = new HttpRange(options.Offset, options.Length),
            Conditions = new BlobRequestConditions { IfMatch = new ETag(etag) }
        }, cancellationToken);
        return download.Value.Content;
    });

    public Task<StorageObjectInfo> WriteObjectAsync(string path, Stream content, StorageWriteOptions? options = null, CancellationToken cancellationToken = default) => ExecuteAsync(async () =>
    {
        options ??= new StorageWriteOptions();
        var response = await container.GetBlobClient(path).UploadAsync(content, new BlobUploadOptions
        {
            Conditions = Conditions(options),
            HttpHeaders = Headers(options),
            Metadata = Copy(options.Metadata),
            TransferOptions = new global::Azure.Storage.StorageTransferOptions { MaximumConcurrency = 1, InitialTransferSize = 4 * 1024 * 1024, MaximumTransferSize = 4 * 1024 * 1024 }
        }, cancellationToken);
        return await ReadWrittenInfoAsync(path, response.Value.ETag, cancellationToken);
    });

    public Task SetObjectMetadataAsync(string path, IReadOnlyDictionary<string, string> metadata, string? ifMatch = null, CancellationToken cancellationToken = default) => ExecuteAsync(async () =>
    {
        await container.GetBlobClient(path).SetMetadataAsync(Copy(metadata), new BlobRequestConditions { IfMatch = ETagOrNull(ifMatch) }, cancellationToken);
        return true;
    });

    public Task<bool> DeleteObjectIfExistsAsync(string path, bool includeSnapshots = false, CancellationToken cancellationToken = default) => ExecuteAsync(async () =>
        (await container.GetBlobClient(path).DeleteIfExistsAsync(includeSnapshots ? DeleteSnapshotsOption.IncludeSnapshots : DeleteSnapshotsOption.None,
            cancellationToken: cancellationToken)).Value);

    public Task<StorageObjectPage> ListObjectsAsync(string? prefix = null, string? continuationToken = null, int pageSize = 100, CancellationToken cancellationToken = default) => ExecuteAsync(async () =>
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(pageSize, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(pageSize, 5000);
        await foreach (var page in container.GetBlobsAsync(new GetBlobsOptions { Prefix = prefix, Traits = BlobTraits.Metadata }, cancellationToken)
                           .AsPages(continuationToken, pageSize))
        {
            return new StorageObjectPage(page.Values.Select(item => new StorageObjectInfo(item.Name,
                item.Properties.ETag?.ToString() ?? throw new InvalidDataException("Object listing returned no ETag."),
                item.Properties.ContentLength ?? throw new InvalidDataException("Object listing returned no length."),
                item.Properties.ContentType, item.Properties.ContentEncoding, new Dictionary<string, string>(item.Metadata), item.Properties.LastModified)).ToArray(), page.ContinuationToken);
        }
        return new StorageObjectPage([], null);
    });

    public Task StagePartAsync(string path, string partId, Stream content, CancellationToken cancellationToken = default) => ExecuteAsync(async () =>
    {
        await container.GetBlockBlobClient(path).StageBlockAsync(partId, content, cancellationToken: cancellationToken);
        return true;
    });

    public Task<StorageObjectInfo> CommitPartsAsync(string path, IReadOnlyList<string> partIds, StorageWriteOptions? options = null, CancellationToken cancellationToken = default) => ExecuteAsync(async () =>
    {
        options ??= new StorageWriteOptions();
        var response = await container.GetBlockBlobClient(path).CommitBlockListAsync(partIds, new CommitBlockListOptions
        {
            Conditions = Conditions(options),
            HttpHeaders = Headers(options),
            Metadata = Copy(options.Metadata)
        }, cancellationToken);
        return await ReadWrittenInfoAsync(path, response.Value.ETag, cancellationToken);
    });

    private async Task<StorageObjectInfo> ReadWrittenInfoAsync(string path, ETag etag, CancellationToken cancellationToken) =>
        Info(path, (await container.GetBlobClient(path).GetPropertiesAsync(new BlobRequestConditions { IfMatch = etag }, cancellationToken)).Value);

    private static StorageObjectInfo Info(string path, BlobProperties value) => new(path, value.ETag.ToString(), value.ContentLength,
        value.ContentType, value.ContentEncoding, new Dictionary<string, string>(value.Metadata), value.LastModified);

    private static BlobRequestConditions Conditions(StorageWriteOptions options)
    {
        if (options.IfAbsent && options.IfMatch is not null) throw new ArgumentException("IfAbsent and IfMatch are mutually exclusive.");
        return new BlobRequestConditions { IfMatch = ETagOrNull(options.IfMatch), IfNoneMatch = options.IfAbsent ? ETag.All : null };
    }

    private static BlobHttpHeaders Headers(StorageWriteOptions options) => new() { ContentType = options.ContentType, ContentEncoding = options.ContentEncoding };
    private static ETag? ETagOrNull(string? value) => value is null ? null : new ETag(value);
    private static Dictionary<string, string>? Copy(IReadOnlyDictionary<string, string>? value) => value is null ? null : new(value);

    private static async Task<T> ExecuteAsync<T>(Func<Task<T>> operation)
    {
        try { return await operation(); }
        catch (RequestFailedException exception)
        {
            throw new StorageOperationException("Object storage operation failed.", exception.Status, exception, exception.ErrorCode);
        }
    }
}
