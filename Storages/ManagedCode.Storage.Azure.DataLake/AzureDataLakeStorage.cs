using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Azure.Storage.Files.DataLake;
using Azure.Storage.Files.DataLake.Models;
using ManagedCode.Communication;
using ManagedCode.Storage.Azure.DataLake.Options;
using ManagedCode.Storage.Core;
using ManagedCode.Storage.Core.Models;
using Microsoft.Extensions.Logging;

namespace ManagedCode.Storage.Azure.DataLake;

public class AzureDataLakeStorage : BaseStorage<DataLakeFileSystemClient, AzureDataLakeStorageOptions>, IAzureDataLakeStorage
{
    private readonly DataLakeServiceClient _dataLakeServiceClient;
    private readonly ILogger<AzureDataLakeStorage>? _logger;

    public AzureDataLakeStorage(AzureDataLakeStorageOptions options, ILogger<AzureDataLakeStorage>? logger = null) : base(options)
    {
        _logger = logger;
        _dataLakeServiceClient = new DataLakeServiceClient(options.ConnectionString);
    }

    public DataLakeFileSystemClient StorageClient { get; }

    public override async Task<Result> RemoveContainerAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await _dataLakeServiceClient.DeleteFileSystemAsync(StorageOptions.FileSystem, cancellationToken: cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            return Result.Succeed();
        }
        catch (Exception ex)
        {
            _logger.LogException(ex);
            return Result.Fail(ex);
        }
    }

    public async Task<Result<Stream>> OpenReadStreamAsync(OpenReadStreamOptions options, CancellationToken cancellationToken = default)
    {
        try
        {
            var fileClient = GetFileClient(options);
            var stream = await fileClient.OpenReadAsync(options.Position, options.BufferSize, cancellationToken: cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            return Result.Succeed(stream);
        }
        catch (Exception ex)
        {
            _logger.LogException(ex);
            return Result<Stream>.Fail(ex);
        }
    }

    public async Task<Result<Stream>> OpenWriteStreamAsync(OpenWriteStreamOptions options, CancellationToken cancellationToken = default)
    {
        try
        {
            var fileClient = GetFileClient(options);
            var stream = await fileClient.OpenWriteAsync(options.Overwrite, cancellationToken: cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            return Result.Succeed(stream);
        }
        catch (Exception ex)
        {
            _logger.LogException(ex);
            return Result<Stream>.Fail(ex);
        }
    }

    public override async IAsyncEnumerable<BlobMetadata> GetBlobMetadataListAsync(string? directory = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await foreach (var item in StorageClient.GetPathsAsync(directory, cancellationToken: cancellationToken))
        {
            if (item is not null)
            {
                yield return new BlobMetadata
                {
                    Name = item.Name
                };
            }

            if (cancellationToken.IsCancellationRequested)
                yield break;
        }
    }

    public async Task<Result> CreateDirectoryAsync(string directory, CancellationToken cancellationToken = default)
    {
        try
        {
            await StorageClient.CreateDirectoryAsync(directory, cancellationToken: cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            return Result.Succeed();
        }
        catch (Exception ex)
        {
            _logger.LogException(ex);
            return Result<Stream>.Fail(ex);
        }
    }

    public async Task<Result> RenameDirectory(string directory, string newDirectory, CancellationToken cancellationToken = default)
    {
        try
        {
            var directoryClient = StorageClient.GetDirectoryClient(directory);
            cancellationToken.ThrowIfCancellationRequested();
            await directoryClient.RenameAsync(newDirectory, cancellationToken: cancellationToken);
            return Result.Succeed();
        }
        catch (Exception ex)
        {
            _logger.LogException(ex);
            return Result<Stream>.Fail(ex);
        }
    }

    public override async Task<Result<Stream>> GetStreamAsync(string fileName, CancellationToken cancellationToken = default)
    {
        return await OpenReadStreamAsync(new OpenReadStreamOptions
        {
            FileName = fileName,
            Position = 0,
            BufferSize = 4096
        }, cancellationToken);
    }

    protected override DataLakeFileSystemClient CreateStorageClient()
    {
        return _dataLakeServiceClient.GetFileSystemClient(StorageOptions.FileSystem);
    }

    protected override async Task<Result> CreateContainerInternalAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await _dataLakeServiceClient.CreateFileSystemAsync(StorageOptions.FileSystem, StorageOptions.PublicAccessType, cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            return Result.Succeed();
        }
        catch (Exception ex)
        {
            _logger.LogException(ex);
            return Result.Fail(ex);
        }
    }

    protected override async Task<Result<BlobMetadata>> UploadInternalAsync(Stream stream, UploadOptions options,
        CancellationToken cancellationToken = default)
    {
        try
        {
            DataLakeFileUploadOptions dataLakeFileUploadOptions = new()
            {
                HttpHeaders = new PathHttpHeaders
                {
                    ContentType = options.MimeType
                }
            };

            var fileClient = GetFileClient(options);
            await fileClient.UploadAsync(stream, dataLakeFileUploadOptions, cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            return await GetBlobMetadataInternalAsync(MetadataOptions.FromBaseOptions(options), cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogException(ex);
            return Result<BlobMetadata>.Fail(ex);
        }
    }

    protected override async Task<Result<LocalFile>> DownloadInternalAsync(LocalFile localFile, DownloadOptions options,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var fileClient = GetFileClient(options);
            var downloadResponse = await fileClient.ReadAsync(cancellationToken);
            var reader = new BinaryReader(downloadResponse.Value.Content);
            var fileStream = localFile.FileStream;

            var bufferSize = 4096;
            var buffer = new byte[bufferSize];
            int count;

            while ((count = reader.Read(buffer, 0, buffer.Length)) != 0)
                await fileStream.WriteAsync(buffer, 0, count, cancellationToken);

            await fileStream.FlushAsync(cancellationToken);

            fileStream.Close();
            cancellationToken.ThrowIfCancellationRequested();

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
            var fileClient = GetFileClient(options);
            await fileClient.DeleteAsync(cancellationToken: cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            return Result.Succeed(true);
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
            var fileClient = GetFileClient(options);
            var result = await fileClient.ExistsAsync(cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            return Result.Succeed(result.Value);
        }
        catch (Exception ex)
        {
            _logger.LogException(ex);
            return Result<bool>.Fail(ex);
        }
    }

    protected override async Task<Result<BlobMetadata>> GetBlobMetadataInternalAsync(MetadataOptions options,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var fileClient = GetFileClient(options);
            var properties = await fileClient.GetPropertiesAsync(cancellationToken: cancellationToken);

            // TODO: Check it
            cancellationToken.ThrowIfCancellationRequested();
            return Result<BlobMetadata>.Succeed(new BlobMetadata
            {
                Name = fileClient.Name,
                Uri = fileClient.Uri,
                Container = fileClient.FileSystemName,
                Length = (ulong)properties.Value.ContentLength,
                CreatedOn = properties.Value.CreatedOn,
                LastModified = properties.Value.LastModified,
                Metadata = properties.Value.Metadata.ToDictionary(k => k.Key, v => v.Value),
                MimeType = properties.Value.ContentType
            });
        }
        catch (Exception ex)
        {
            _logger.LogException(ex);
            return Result<BlobMetadata>.Fail(ex);
        }
    }

    protected override async Task<Result> DeleteDirectoryInternalAsync(string directory, CancellationToken cancellationToken = default)
    {
        try
        {
            await StorageClient.DeleteDirectoryAsync(directory, cancellationToken: cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            return Result.Succeed();
        }
        catch (Exception ex)
        {
            _logger.LogException(ex);
            return Result<bool>.Fail(ex);
        }

    }

    protected override Task<Result> SetLegalHoldInternalAsync(bool hasLegalHold, LegalHoldOptions options,
        CancellationToken cancellationToken = default)
    {
        return Result.Fail(new NotSupportedException("Legal hold is not supported by Data Lake Storage"))
            .AsTask();
    }

    protected override Task<Result<bool>> HasLegalHoldInternalAsync(LegalHoldOptions options, CancellationToken cancellationToken = default)
    {
        return Result<bool>.Fail(new NotSupportedException("Legal hold is not supported by Data Lake Storage"))
            .AsTask();
    }

    private DataLakeFileClient GetFileClient(BaseOptions options)
    {
        return options.Directory switch
        {
            null => StorageClient.GetFileClient(options.FileName),
            _ => StorageClient.GetDirectoryClient(options.Directory)
                .GetFileClient(options.FileName)
        };
    }
}