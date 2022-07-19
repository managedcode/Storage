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
            var fileClient = directoryClient.GetFileClient(options.FileName);
            _ = await fileClient.UploadAsync(stream);
            return Result.Succeed(string.Empty);
        }
        catch (Exception ex)
        {
            return Result<string>.Fail(ex);
        }
    }

    protected override async Task<Result<LocalFile>> DownloadInternalAsync(LocalFile localFile, string blob,
        CancellationToken cancellationToken = default)
    {
        try
        {
            DataLakeDirectoryClient directoryClient = StorageClient.GetDirectoryClient(Path.GetDirectoryName(blob));
            DataLakeFileClient fileClient = directoryClient.GetFileClient(Path.GetFileName(blob));
            var downloadResponse = await fileClient.ReadAsync(cancellationToken);
            BinaryReader reader = new BinaryReader(downloadResponse.Value.Content);
            FileStream fileStream = localFile.FileStream;

            int bufferSize = 4096;
            byte[] buffer = new byte[bufferSize];
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

    public override async Task<Result<bool>> DeleteAsync(string blob, CancellationToken cancellationToken = default)
    {
        try
        {
            DataLakeDirectoryClient directoryClient = StorageClient.GetDirectoryClient(Path.GetDirectoryName(blob));
            DataLakeFileClient fileClient = directoryClient.GetFileClient(Path.GetFileName(blob));
            await fileClient.DeleteAsync(cancellationToken: cancellationToken);
            return Result.Succeed(true);
        }
        catch (Exception ex)
        {
            return Result<bool>.Fail(ex);
        }
    }

    public override async Task<Result<bool>> ExistsAsync(string blob, CancellationToken cancellationToken = default)
    {
        try
        {
            var directoryClient = StorageClient.GetDirectoryClient(Path.GetDirectoryName(blob));
            var fileClient = directoryClient.GetFileClient(Path.GetFileName(blob));
            var result = await fileClient.ExistsAsync(cancellationToken: cancellationToken);
            return Result.Succeed(result.Value);
        }
        catch (Exception ex)
        {
            return Result<bool>.Fail(ex);
        }
    }

    public override async Task<Result<BlobMetadata>> GetBlobMetadataAsync(string blob, CancellationToken cancellationToken = default)
    {
        try
        {
            try
            {
                var directoryClient = StorageClient.GetDirectoryClient(Path.GetDirectoryName(blob));
                var fileClient = directoryClient.GetFileClient(Path.GetFileName(blob));
                _ = await fileClient.GetPropertiesAsync(cancellationToken: cancellationToken);
                return Result.Succeed(new BlobMetadata()
                {
                    Name = blob
                });
            }
            catch (Exception ex)
            {
                return Result<BlobMetadata>.Fail(ex);
            }
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

    public override Task<Result> SetLegalHoldAsync(string blob, bool hasLegalHold, CancellationToken cancellationToken = default)
    {
        throw new NotSupportedException("Legal hold is not supported by Data Lake Storage");
    }

    public override Task<Result<bool>> HasLegalHoldAsync(string blob, CancellationToken cancellationToken = default)
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