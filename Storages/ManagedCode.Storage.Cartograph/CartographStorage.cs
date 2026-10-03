using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Cartograph.Catalog;
using ManagedCode.Communication;
using ManagedCode.MimeTypes;
using ManagedCode.Storage.Cartograph.Options;
using ManagedCode.Storage.Core;
using ManagedCode.Storage.Core.Models;

namespace ManagedCode.Storage.Cartograph;

/// <summary>Reads files inside a local, immutable Cartograph artifact without extracting their payloads.</summary>
public sealed class CartographStorage(CartographStorageOptions options)
    : BaseStorage<string, CartographStorageOptions>(options), ICartographStorage
{
    protected override string CreateStorageClient()
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(StorageOptions.ArtifactPath);
        return Path.GetFullPath(StorageOptions.ArtifactPath);
    }

    protected override Task<Result> CreateContainerInternalAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            using var artifact = CartographArtifact.Open(StorageClient);
            return Task.FromResult(Result.Succeed());
        }
        catch (Exception exception)
        {
            return Task.FromResult(Result.Fail(exception));
        }
    }

    public override Task<Result<Stream>> GetStreamAsync(string fileName, CancellationToken cancellationToken = default)
    {
        CartographArtifact? artifact = null;
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            artifact = CartographArtifact.Open(StorageClient);
            return Task.FromResult(Result<Stream>.Succeed(artifact.OpenRead(fileName)));
        }
        catch (Exception exception)
        {
            artifact?.Dispose();
            return Task.FromResult(Result<Stream>.Fail(exception));
        }
    }

    protected override async Task<Result<LocalFile>> DownloadInternalAsync(LocalFile localFile, DownloadOptions options,
        CancellationToken cancellationToken = default)
    {
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            var artifactPath = StorageClient;
            if (CartographPath.ResolveFileSystemPath(localFile.FilePath).Equals(CartographPath.ResolveFileSystemPath(artifactPath),
                    OperatingSystem.IsWindows() || OperatingSystem.IsMacOS() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal))
                throw new IOException("A download destination cannot overwrite its source artifact.");

            var artifact = CartographArtifact.Open(artifactPath);
            Stream source;
            try
            {
                var entry = artifact.RequireEntry(options.FullPath);
                localFile.BlobMetadata = CreateMetadata(entry, artifact);
                source = artifact.OpenRead(options.FullPath);
            }
            catch
            {
                artifact.Dispose();
                throw;
            }

            await using (source)
                await CartographDownload.CopyToAsync(source, localFile.FilePath, cancellationToken).ConfigureAwait(false);

            return Result<LocalFile>.Succeed(localFile);
        }
        catch (Exception exception)
        {
            localFile.Dispose();
            return Result<LocalFile>.Fail(exception);
        }
    }

    protected override Task<Result<bool>> ExistsInternalAsync(ExistOptions options, CancellationToken cancellationToken = default)
    {
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            using var artifact = CartographArtifact.Open(StorageClient);
            return Task.FromResult(Result<bool>.Succeed(artifact.Find(options.FullPath) is not null));
        }
        catch (Exception exception)
        {
            return Task.FromResult(Result<bool>.Fail(exception));
        }
    }

    protected override Task<Result<BlobMetadata>> GetBlobMetadataInternalAsync(MetadataOptions options,
        CancellationToken cancellationToken = default)
    {
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            using var artifact = CartographArtifact.Open(StorageClient);
            return Task.FromResult(Result<BlobMetadata>.Succeed(CreateMetadata(artifact.RequireEntry(options.FullPath), artifact)));
        }
        catch (Exception exception)
        {
            return Task.FromResult(Result<BlobMetadata>.Fail(exception));
        }
    }

    public override async IAsyncEnumerable<BlobMetadata> GetBlobMetadataListAsync(string? directory = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await Task.CompletedTask.ConfigureAwait(false);
        cancellationToken.ThrowIfCancellationRequested();
        var prefix = CartographPath.DirectoryPrefix(directory);
        using var artifact = CartographArtifact.Open(StorageClient);
        foreach (var metadata in artifact.Catalog.Entries
                     .Where(entry => entry.RelativePath.StartsWith(prefix, StringComparison.Ordinal))
                     .Select(entry => CreateMetadata(entry, artifact)))
        {
            cancellationToken.ThrowIfCancellationRequested();
            yield return metadata;
        }
    }

    private static BlobMetadata CreateMetadata(CatalogEntry entry, CartographArtifact artifact) => new()
    {
        FullName = entry.RelativePath,
        Name = entry.Name,
        Container = artifact.Path,
        Uri = new UriBuilder(new Uri(artifact.Path)) { Fragment = entry.RelativePath }.Uri,
        MimeType = MimeHelper.GetMimeType(entry.Name),
        Length = (ulong)entry.Length,
        CreatedOn = artifact.Catalog.CreatedUtc,
        LastModified = entry.LastWriteUtc
    };

    private static NotSupportedException ReadOnlyError() => new("Cartograph artifacts are immutable; this provider supports reads only.");

    public override Task<Result> RemoveContainerAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(Result.Fail(ReadOnlyError()));
    protected override Task<Result> DeleteDirectoryInternalAsync(string directory, CancellationToken cancellationToken = default) =>
        Task.FromResult(Result.Fail(ReadOnlyError()));
    protected override Task<Result<BlobMetadata>> UploadInternalAsync(Stream stream, UploadOptions options, CancellationToken cancellationToken = default) =>
        Task.FromResult(Result<BlobMetadata>.Fail(ReadOnlyError()));
    protected override Task<Result<bool>> DeleteInternalAsync(DeleteOptions options, CancellationToken cancellationToken = default) =>
        Task.FromResult(Result<bool>.Fail(ReadOnlyError()));
    protected override Task<Result> SetLegalHoldInternalAsync(bool hasLegalHold, LegalHoldOptions options, CancellationToken cancellationToken = default) =>
        Task.FromResult(Result.Fail(ReadOnlyError()));
    protected override Task<Result<bool>> HasLegalHoldInternalAsync(LegalHoldOptions options, CancellationToken cancellationToken = default) =>
        Task.FromResult(Result<bool>.Fail(ReadOnlyError()));
}
