using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ManagedCode.Communication;
using ManagedCode.Storage.Browser.Interop;
using ManagedCode.Storage.Browser.Models;
using ManagedCode.Storage.Browser.Options;
using ManagedCode.Storage.Core;
using ManagedCode.Storage.Core.Exceptions;
using ManagedCode.Storage.Core.Models;
using Microsoft.JSInterop;

namespace ManagedCode.Storage.Browser;

public sealed class BrowserStorage : BaseStorage<IJSRuntime, BrowserStorageOptions>, IBrowserStorage, IAsyncDisposable
{
    private const string KeyNamespace = "managedcode.browser.indexeddb";
    private readonly BrowserIndexedDbInterop _interop;

    public BrowserStorage(BrowserStorageOptions options) : base(options)
    {
        _interop = new BrowserIndexedDbInterop(StorageClient);
    }

    public override async Task<Result> RemoveContainerAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            await _interop.RemoveContainerAsync(StorageOptions.DatabaseName, ContainerKey, cancellationToken);
            IsContainerCreated = false;
            return Result.Succeed();
        }
        catch (Exception ex)
        {
            return Result.Fail(ex);
        }
    }

    public override async IAsyncEnumerable<BlobMetadata> GetBlobMetadataListAsync(string? directory = null,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var containerResult = await EnsureContainerReadyAsync(cancellationToken);
        if (containerResult.IsFailed)
            yield break;

        var blobs = await _interop.ListBlobsAsync(StorageOptions.DatabaseName, ContainerKey, BuildBlobPrefix(directory), cancellationToken);

        foreach (var blob in blobs.OrderBy(item => item.Key, StringComparer.Ordinal))
        {
            cancellationToken.ThrowIfCancellationRequested();
            yield return ToBlobMetadata(blob);
        }
    }

    public override async Task<Result<Stream>> GetStreamAsync(string fileName, CancellationToken cancellationToken = default)
    {
        try
        {
            var storedBlob = await ReadStoredBlobAsync(new DownloadOptions { FileName = fileName }, cancellationToken);
            if (storedBlob is null)
                return Result<Stream>.Fail("File not found");

            return Result<Stream>.Succeed(CreateReadStream(storedBlob));
        }
        catch (Exception ex)
        {
            return Result<Stream>.Fail(ex);
        }
    }

    protected override IJSRuntime CreateStorageClient()
    {
        return StorageOptions.JsRuntime
               ?? throw new BadConfigurationException("Browser storage requires BrowserStorageOptions.JsRuntime to be configured.");
    }

    protected override async Task<Result> CreateContainerInternalAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            await _interop.CreateContainerAsync(StorageOptions.DatabaseName, ContainerKey, cancellationToken);
            return Result.Succeed();
        }
        catch (Exception ex)
        {
            return Result.Fail(ex);
        }
    }

    protected override async Task<Result> DeleteDirectoryInternalAsync(string directory, CancellationToken cancellationToken = default)
    {
        try
        {
            var containerResult = await EnsureContainerReadyAsync(cancellationToken);
            if (containerResult.IsFailed)
                return containerResult;

            await _interop.DeleteByPrefixAsync(StorageOptions.DatabaseName, ContainerKey, BuildBlobPrefix(directory), cancellationToken);
            return Result.Succeed();
        }
        catch (Exception ex)
        {
            return Result.Fail(ex);
        }
    }

    protected override async Task<Result<BlobMetadata>> UploadInternalAsync(Stream stream, UploadOptions options,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var containerResult = await EnsureContainerReadyAsync(cancellationToken);
            if (containerResult.IsFailed)
                return Result<BlobMetadata>.Fail(containerResult.Problem);

            var existing = await ReadStoredBlobAsync(options, cancellationToken);
            var blobKey = BuildBlobKey(options);
            var payloadKey = CreatePayloadKey(blobKey);
            var payloadWritten = false;
            BrowserPayloadWriteResult writeResult;

            var now = DateTimeOffset.UtcNow;
            var uploader = new BrowserChunkUploadSession(_interop, StorageOptions);
            try
            {
                writeResult = await uploader.WriteAsync(stream, StorageOptions.DatabaseName, payloadKey, cancellationToken)
                    .ConfigureAwait(false);
                payloadWritten = true;

                var storedBlob = new BrowserStoredBlob
                {
                    Key = blobKey,
                    PayloadKey = payloadKey,
                    Container = ContainerKey,
                    FullName = NormalizePath(options.FullPath),
                    Name = Path.GetFileName(options.FileName),
                    Directory = NormalizeDirectory(options.Directory),
                    MimeType = options.MimeType,
                    Metadata = options.Metadata,
                    Length = (ulong)writeResult.Length,
                    ChunkSizeBytes = StorageOptions.ChunkSizeBytes,
                    PayloadStore = writeResult.PayloadStore,
                    CreatedOn = existing?.CreatedOn ?? now,
                    LastModified = now,
                    HasLegalHold = existing?.HasLegalHold ?? false
                };

                await _interop.PutBlobAsync(StorageOptions.DatabaseName, storedBlob, cancellationToken);
                await DeleteSupersededPayloadAsync(existing, payloadKey, cancellationToken);
                return Result<BlobMetadata>.Succeed(ToBlobMetadata(storedBlob));
            }
            catch
            {
                if (payloadWritten)
                    await TryDeletePayloadAsync(payloadKey, cancellationToken);

                throw;
            }
        }
        catch (Exception ex)
        {
            return Result<BlobMetadata>.Fail(ex);
        }
    }

    protected override async Task<Result<LocalFile>> DownloadInternalAsync(LocalFile localFile, DownloadOptions options,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var storedBlob = await ReadStoredBlobAsync(options, cancellationToken);
            if (storedBlob is null)
                return Result<LocalFile>.Fail("File not found");

            await using var stream = CreateReadStream(storedBlob);

            await localFile.CopyFromStreamAsync(stream, cancellationToken);
            localFile.BlobMetadata = ToBlobMetadata(storedBlob);
            return Result<LocalFile>.Succeed(localFile);
        }
        catch (Exception ex)
        {
            return Result<LocalFile>.Fail(ex);
        }
    }

    protected override async Task<Result<bool>> DeleteInternalAsync(DeleteOptions options, CancellationToken cancellationToken = default)
    {
        try
        {
            var containerResult = await EnsureContainerReadyAsync(cancellationToken);
            if (containerResult.IsFailed)
                return Result<bool>.Fail(containerResult.Problem);

            var deleted = await _interop.DeleteBlobAsync(StorageOptions.DatabaseName, BuildBlobKey(options), cancellationToken);
            return Result<bool>.Succeed(deleted);
        }
        catch (Exception ex)
        {
            return Result<bool>.Fail(ex);
        }
    }

    protected override async Task<Result<bool>> ExistsInternalAsync(ExistOptions options, CancellationToken cancellationToken = default)
    {
        try
        {
            var containerResult = await EnsureContainerReadyAsync(cancellationToken);
            if (containerResult.IsFailed)
                return Result<bool>.Fail(containerResult.Problem);

            var storedBlob = await _interop.GetBlobAsync(StorageOptions.DatabaseName, BuildBlobKey(options), cancellationToken);
            return Result<bool>.Succeed(storedBlob is not null);
        }
        catch (Exception ex)
        {
            return Result<bool>.Fail(ex);
        }
    }

    protected override async Task<Result<BlobMetadata>> GetBlobMetadataInternalAsync(MetadataOptions options,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var storedBlob = await ReadStoredBlobAsync(options, cancellationToken);
            return storedBlob is null
                ? Result<BlobMetadata>.Fail("File not found")
                : Result<BlobMetadata>.Succeed(ToBlobMetadata(storedBlob));
        }
        catch (Exception ex)
        {
            return Result<BlobMetadata>.Fail(ex);
        }
    }

    protected override async Task<Result> SetLegalHoldInternalAsync(bool hasLegalHold, LegalHoldOptions options,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var storedBlob = await ReadStoredBlobAsync(options, cancellationToken);
            if (storedBlob is null)
                return Result.Fail("File not found");

            storedBlob.HasLegalHold = hasLegalHold;
            storedBlob.LastModified = DateTimeOffset.UtcNow;
            await _interop.PutBlobAsync(StorageOptions.DatabaseName, storedBlob, cancellationToken);
            return Result.Succeed();
        }
        catch (Exception ex)
        {
            return Result.Fail(ex);
        }
    }

    protected override async Task<Result<bool>> HasLegalHoldInternalAsync(LegalHoldOptions options, CancellationToken cancellationToken = default)
    {
        try
        {
            var storedBlob = await ReadStoredBlobAsync(options, cancellationToken);
            return Result<bool>.Succeed(storedBlob?.HasLegalHold ?? false);
        }
        catch (Exception ex)
        {
            return Result<bool>.Fail(ex);
        }
    }

    private string ContainerKey => $"{KeyNamespace}::{NormalizePath(StorageOptions.ContainerName)}";

    public ValueTask DisposeAsync()
    {
        return _interop.DisposeAsync();
    }

    private async Task<Result> EnsureContainerReadyAsync(CancellationToken cancellationToken)
    {
        if (IsContainerCreated)
            return Result.Succeed();

        var exists = await _interop.ContainerExistsAsync(StorageOptions.DatabaseName, ContainerKey, cancellationToken);
        if (exists)
        {
            IsContainerCreated = true;
            return Result.Succeed();
        }

        if (!StorageOptions.CreateContainerIfNotExists)
            return Result.Fail("Browser storage container does not exist.");

        return await CreateContainerAsync(cancellationToken);
    }

    private async Task<BrowserStoredBlob?> ReadStoredBlobAsync(BaseOptions options, CancellationToken cancellationToken)
    {
        var containerResult = await EnsureContainerReadyAsync(cancellationToken);
        if (containerResult.IsFailed)
            return null;

        return await _interop.GetBlobAsync(StorageOptions.DatabaseName, BuildBlobKey(options), cancellationToken);
    }

    private string BuildBlobKey(BaseOptions options)
    {
        return $"{ContainerKey}::{NormalizePath(options.FullPath)}";
    }

    private string BuildBlobPrefix(string? directory)
    {
        var normalizedDirectory = NormalizeDirectory(directory);
        return string.IsNullOrWhiteSpace(normalizedDirectory)
            ? $"{ContainerKey}::"
            : $"{ContainerKey}::{normalizedDirectory}/";
    }

    private static string NormalizePath(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        return path.Replace('\\', '/').Trim('/');
    }

    private static string? NormalizeDirectory(string? directory)
    {
        return string.IsNullOrWhiteSpace(directory)
            ? null
            : NormalizePath(directory);
    }

    private static BlobMetadata ToBlobMetadata(BrowserStoredBlob storedBlob)
    {
        return new BlobMetadata
        {
            FullName = storedBlob.FullName,
            Name = storedBlob.Name,
            Metadata = storedBlob.Metadata,
            MimeType = storedBlob.MimeType,
            CreatedOn = storedBlob.CreatedOn,
            LastModified = storedBlob.LastModified,
            Length = storedBlob.Length
        };
    }

    private Stream CreateReadStream(BrowserStoredBlob storedBlob)
    {
        if (!string.Equals(storedBlob.PayloadStore, BrowserPayloadStores.Opfs, StringComparison.Ordinal))
        {
            throw new NotSupportedException(
                $"Browser payload '{storedBlob.FullName}' is not OPFS-backed. This provider supports OPFS-backed payloads only.");
        }

        return new BrowserStorageOpfsReadStream(
            _interop,
            StorageOptions.DatabaseName,
            ResolvePayloadKey(storedBlob),
            storedBlob.Length,
            storedBlob.ChunkSizeBytes,
            StorageOptions.ChunkBatchSize);
    }

    private static string CreatePayloadKey(string blobKey)
    {
        return $"{blobKey}::payload::{Guid.NewGuid():N}";
    }

    private static string ResolvePayloadKey(BrowserStoredBlob storedBlob)
    {
        return string.IsNullOrWhiteSpace(storedBlob.PayloadKey)
            ? storedBlob.Key
            : storedBlob.PayloadKey;
    }

    private async Task DeleteSupersededPayloadAsync(BrowserStoredBlob? existing, string currentPayloadKey, CancellationToken cancellationToken)
    {
        if (existing is null)
            return;

        var previousPayloadKey = ResolvePayloadKey(existing);
        if (string.Equals(previousPayloadKey, currentPayloadKey, StringComparison.Ordinal))
            return;

        await TryDeletePayloadAsync(previousPayloadKey, cancellationToken);
    }

    private async Task TryDeletePayloadAsync(string payloadKey, CancellationToken cancellationToken)
    {
        try
        {
            await _interop.DeletePayloadFileAsync(StorageOptions.DatabaseName, payloadKey, cancellationToken);
        }
        catch
        {
        }
    }
}
