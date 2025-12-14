using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using ManagedCode.Communication;
using ManagedCode.MimeTypes;
using ManagedCode.Storage.Core;
using ManagedCode.Storage.Core.Models;
using ManagedCode.Storage.Dropbox.Clients;
using ManagedCode.Storage.Dropbox.Options;
using Microsoft.Extensions.Logging;

namespace ManagedCode.Storage.Dropbox;

public class DropboxStorage : BaseStorage<IDropboxClientWrapper, DropboxStorageOptions>, IDropboxStorage
{
    private readonly ILogger<DropboxStorage>? _logger;

    public DropboxStorage(DropboxStorageOptions storageOptions, ILogger<DropboxStorage>? logger = null) : base(storageOptions)
    {
        _logger = logger;
    }

    protected override IDropboxClientWrapper CreateStorageClient()
    {
        if (StorageOptions.Client != null)
        {
            return StorageOptions.Client;
        }

        if (StorageOptions.DropboxClient != null)
        {
            return new DropboxClientWrapper(StorageOptions.DropboxClient);
        }

        throw new InvalidOperationException("Dropbox client is not configured for storage.");
    }

    protected override async Task<Result> CreateContainerInternalAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await StorageClient.EnsureRootAsync(StorageOptions.RootPath, StorageOptions.CreateContainerIfNotExists, cancellationToken);
            IsContainerCreated = true;
            return Result.Succeed();
        }
        catch (Exception ex)
        {
            _logger.LogException(ex);
            return Result.Fail(ex);
        }
    }

    public override Task<Result> RemoveContainerAsync(CancellationToken cancellationToken = default)
    {
        // Dropbox API does not expose a direct container deletion concept; callers manage folders explicitly.
        return Task.FromResult(Result.Succeed());
    }

    protected override async Task<Result> DeleteDirectoryInternalAsync(string directory, CancellationToken cancellationToken = default)
    {
        try
        {
            await EnsureContainerExist(cancellationToken);
            var normalizedDirectory = NormalizeRelativePath(directory);

            if (!string.IsNullOrWhiteSpace(normalizedDirectory))
            {
                _ = await StorageClient.DeleteAsync(StorageOptions.RootPath, normalizedDirectory, cancellationToken);
                return Result.Succeed();
            }

            await foreach (var item in StorageClient.ListAsync(StorageOptions.RootPath, null, cancellationToken))
            {
                _ = await StorageClient.DeleteAsync(StorageOptions.RootPath, item.Name, cancellationToken);
            }

            return Result.Succeed();
        }
        catch (Exception ex)
        {
            _logger.LogException(ex);
            return Result.Fail(ex);
        }
    }

    protected override async Task<Result<BlobMetadata>> UploadInternalAsync(Stream stream, UploadOptions options, CancellationToken cancellationToken = default)
    {
        try
        {
            await EnsureContainerExist(cancellationToken);
            var path = BuildFullPath(options.FullPath);
            var uploaded = await StorageClient.UploadAsync(StorageOptions.RootPath, path, stream, options.MimeType, cancellationToken);
            return Result<BlobMetadata>.Succeed(ToBlobMetadata(uploaded, path));
        }
        catch (Exception ex)
        {
            _logger.LogException(ex);
            return Result<BlobMetadata>.Fail(ex);
        }
    }

    protected override async Task<Result<LocalFile>> DownloadInternalAsync(LocalFile localFile, DownloadOptions options, CancellationToken cancellationToken = default)
    {
        try
        {
            await EnsureContainerExist(cancellationToken);
            var path = BuildFullPath(options.FullPath);
            var remoteStream = await StorageClient.DownloadAsync(StorageOptions.RootPath, path, cancellationToken);

            await using (remoteStream)
            await using (var fileStream = localFile.FileStream)
            {
                await remoteStream.CopyToAsync(fileStream, cancellationToken);
                fileStream.Position = 0;
            }

            return Result<LocalFile>.Succeed(localFile);
        }
        catch (Exception ex)
        {
            _logger.LogException(ex);
            return Result<LocalFile>.Fail(ex);
        }
    }

    protected override async Task<Result<bool>> DeleteInternalAsync(DeleteOptions options, CancellationToken cancellationToken = default)
    {
        try
        {
            await EnsureContainerExist(cancellationToken);
            var path = BuildFullPath(options.FullPath);
            var deleted = await StorageClient.DeleteAsync(StorageOptions.RootPath, path, cancellationToken);
            return Result<bool>.Succeed(deleted);
        }
        catch (Exception ex)
        {
            _logger.LogException(ex);
            return Result<bool>.Fail(ex);
        }
    }

    protected override async Task<Result<bool>> ExistsInternalAsync(ExistOptions options, CancellationToken cancellationToken = default)
    {
        try
        {
            await EnsureContainerExist(cancellationToken);
            var path = BuildFullPath(options.FullPath);
            var exists = await StorageClient.ExistsAsync(StorageOptions.RootPath, path, cancellationToken);
            return Result<bool>.Succeed(exists);
        }
        catch (Exception ex)
        {
            _logger.LogException(ex);
            return Result<bool>.Fail(ex);
        }
    }

    protected override async Task<Result<BlobMetadata>> GetBlobMetadataInternalAsync(MetadataOptions options, CancellationToken cancellationToken = default)
    {
        try
        {
            await EnsureContainerExist(cancellationToken);
            var path = BuildFullPath(options.FullPath);
            var item = await StorageClient.GetMetadataAsync(StorageOptions.RootPath, path, cancellationToken);
            if (item == null)
            {
                return Result<BlobMetadata>.Fail(new FileNotFoundException(path));
            }

            return Result<BlobMetadata>.Succeed(ToBlobMetadata(item, path));
        }
        catch (Exception ex)
        {
            _logger.LogException(ex);
            return Result<BlobMetadata>.Fail(ex);
        }
    }

    public override async IAsyncEnumerable<BlobMetadata> GetBlobMetadataListAsync(string? directory = null, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await EnsureContainerExist(cancellationToken);
        var normalizedDirectory = string.IsNullOrWhiteSpace(directory) ? null : NormalizeRelativePath(directory!);

        await foreach (var item in StorageClient.ListAsync(StorageOptions.RootPath, normalizedDirectory, cancellationToken))
        {
            var fullPath = normalizedDirectory == null ? item.Name : $"{normalizedDirectory}/{item.Name}";
            yield return ToBlobMetadata(item, fullPath);
        }
    }

    public override async Task<Result<Stream>> GetStreamAsync(string fileName, CancellationToken cancellationToken = default)
    {
        try
        {
            await EnsureContainerExist(cancellationToken);
            var path = BuildFullPath(fileName);
            var stream = await StorageClient.DownloadAsync(StorageOptions.RootPath, path, cancellationToken);
            return Result<Stream>.Succeed(stream);
        }
        catch (Exception ex)
        {
            _logger.LogException(ex);
            return Result<Stream>.Fail(ex);
        }
    }

    protected override Task<Result> SetLegalHoldInternalAsync(bool hasLegalHold, LegalHoldOptions options, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(Result.Succeed());
    }

    protected override Task<Result<bool>> HasLegalHoldInternalAsync(LegalHoldOptions options, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(Result<bool>.Succeed(false));
    }

    private string BuildFullPath(string? relativePath)
    {
        return NormalizeRelativePath(relativePath ?? string.Empty);
    }

    private static string NormalizeRelativePath(string path)
    {
        return path.Replace("\\", "/").Trim('/');
    }

    private BlobMetadata ToBlobMetadata(DropboxItemMetadata file, string fullName)
    {
        return new BlobMetadata
        {
            Name = file.Name,
            FullName = fullName,
            Container = StorageOptions.RootPath,
            Uri = new Uri($"https://www.dropbox.com/home/{file.Path.Trim('/')}", UriKind.RelativeOrAbsolute),
            CreatedOn = file.ClientModified,
            LastModified = file.ServerModified,
            Length = file.Size,
            MimeType = MimeHelper.GetMimeType(file.Name)
        };
    }
}
