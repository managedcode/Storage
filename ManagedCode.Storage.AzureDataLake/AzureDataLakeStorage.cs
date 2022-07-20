using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Azure.Storage.Files.DataLake;
using Azure.Storage.Files.DataLake.Models;
using ManagedCode.Communication;
using ManagedCode.Storage.AzureDataLake.Options;
using ManagedCode.Storage.Core;
using ManagedCode.Storage.Core.Models;
using Microsoft.Extensions.Logging;

namespace ManagedCode.Storage.AzureDataLake;

public class AzureDataLakeStorage : BaseStorage<AzureDataLakeStorageOptions>, IAzureDataLakeStorage
{
    private readonly ILogger<AzureDataLakeStorage> _logger;
    private readonly DataLakeServiceClient _dataLakeServiceClient;

    public AzureDataLakeStorage(ILogger<AzureDataLakeStorage> logger, AzureDataLakeStorageOptions options) : base(options)
    {
        _logger = logger;
        _dataLakeServiceClient = new DataLakeServiceClient(options.ConnectionString);
        StorageClient = _dataLakeServiceClient.GetFileSystemClient(options.FileSystem);
    }

    public DataLakeFileSystemClient StorageClient { get; }

    protected override async Task<Result> CreateContainerInternalAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _ = await _dataLakeServiceClient.CreateFileSystemAsync(StorageOptions.FileSystem, StorageOptions.PublicAccessType,
                cancellationToken: cancellationToken);
            return Result.Succeed();
        }
        catch (Exception e)
        {
            _logger.LogError(e.Message, e);
            return Result.Fail(e);
        }
    }

    public override async Task<Result> RemoveContainerAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _ = await _dataLakeServiceClient.DeleteFileSystemAsync(StorageOptions.FileSystem, cancellationToken: cancellationToken);
            return Result.Succeed();
        }
        catch (Exception e)
        {
            _logger.LogError(e.Message, e);
            return Result.Fail(e);
        }
    }

    protected override async Task<Result<string>> UploadInternalAsync(Stream stream, UploadOptions options,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var directoryClient = StorageClient.GetDirectoryClient(options.Directory);
            var fileClient = directoryClient.GetFileClient(options.Blob);
            _ = await fileClient.UploadAsync(stream);
            return Result.Succeed(string.Empty);
        }
        catch (Exception ex)
        {
            return Result<string>.Fail(ex);
        }
    }


    protected override async Task<Result<LocalFile>> DownloadInternalAsync(LocalFile localFile, DownloadOptions options,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var directoryClient = StorageClient.GetDirectoryClient(Path.GetDirectoryName(options.Directory));
            var fileClient = directoryClient.GetFileClient(Path.GetFileName(options.Blob));
            var downloadResponse = await fileClient.ReadAsync(cancellationToken);
            var reader = new BinaryReader(downloadResponse.Value.Content);
            var fileStream = localFile.FileStream;

            var bufferSize = 4096;
            var buffer = new byte[bufferSize];
            int count;
            while ((count = reader.Read(buffer, 0, buffer.Length)) != 0)
            {
                await fileStream.WriteAsync(buffer, 0, count, cancellationToken);
            }

            await fileStream.FlushAsync(cancellationToken);
            fileStream.Close();
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
            var directoryClient = StorageClient.GetDirectoryClient(Path.GetDirectoryName(options.Directory));
            var fileClient = directoryClient.GetFileClient(Path.GetFileName(options.Blob));
            await fileClient.DeleteAsync(cancellationToken: cancellationToken);
            return Result.Succeed(true);
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
            var directoryClient = StorageClient.GetDirectoryClient(Path.GetDirectoryName(options.Directory));
            var fileClient = directoryClient.GetFileClient(Path.GetFileName(options.Blob));
            var result = await fileClient.ExistsAsync(cancellationToken: cancellationToken);
            return Result.Succeed(result.Value);
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
            var directoryClient = StorageClient.GetDirectoryClient(Path.GetDirectoryName(options.Directory));
            var fileClient = directoryClient.GetFileClient(Path.GetFileName(options.Blob));
            _ = await fileClient.GetPropertiesAsync(cancellationToken: cancellationToken);

            // TODO: Check it
            return Result.Succeed(new BlobMetadata()
            {
                Name = options.Blob
            });
        }
        catch (Exception ex)
        {
            return Result<BlobMetadata>.Fail(ex);
        }
    }


    public async IAsyncEnumerable<BlobMetadata> GetBlobMetadataListAsync(string directory,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        IAsyncEnumerator<PathItem> enumerator =
            StorageClient.GetPathsAsync(directory, cancellationToken: cancellationToken).GetAsyncEnumerator(cancellationToken);
        await enumerator.MoveNextAsync();
        var item = enumerator.Current;

        while (item is not null)
        {
            yield return new BlobMetadata
            {
                Name = item.Name
            };

            if (!await enumerator.MoveNextAsync())
            {
                break;
            }

            item = enumerator.Current;
        }
    }

    public override async IAsyncEnumerable<BlobMetadata> GetBlobMetadataListAsync(
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        // TODO: Implement
        yield return new BlobMetadata();
    }

    protected override Task<Result> SetLegalHoldInternalAsync(bool hasLegalHold, LegalHoldOptions options,
        CancellationToken cancellationToken = default)
    {
        throw new NotSupportedException("Legal hold is not supported by Data Lake Storage");
    }

    protected override Task<Result<bool>> HasLegalHoldInternalAsync(LegalHoldOptions options, CancellationToken cancellationToken = default)
    {
        throw new NotSupportedException("Legal hold is not supported by Data Lake Storage");
    }

    public async Task<Result> CreateDirectoryAsync(string directory, CancellationToken cancellationToken = default)
    {
        _ = await StorageClient.CreateDirectoryAsync(directory, cancellationToken: cancellationToken);
        return Result.Succeed();
    }

    public async Task<Result> RenameDirectory(string directory, string newDirectory, CancellationToken cancellationToken = default)
    {
        var directoryClient = StorageClient.GetDirectoryClient(directory);
        _ = await directoryClient.RenameAsync(newDirectory, cancellationToken: cancellationToken);
        return Result.Succeed();
    }

    public async Task<Result> DeleteDirectory(string directory, CancellationToken cancellationToken = default)
    {
        _ = await StorageClient.DeleteDirectoryAsync(directory, cancellationToken: cancellationToken);
        return Result.Succeed();
    }
}