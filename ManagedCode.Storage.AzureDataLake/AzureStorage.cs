using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Azure.Storage.Files.DataLake;
using Azure.Storage.Files.DataLake.Models;
using ManagedCode.Communication;
using ManagedCode.Storage.Azure.Options;
using ManagedCode.Storage.Core;
using ManagedCode.Storage.Core.Models;
using Microsoft.Extensions.Logging;

namespace ManagedCode.Storage.Azure;

public class AzureDataLake : BaseStorage<AzureDataLakeStorageOptions>, IAzureDataLakeStorage
{
    private readonly ILogger<AzureDataLake> _logger;
    private readonly DataLakeServiceClient _dataLakeServiceClient;
    public AzureDataLake(ILogger<AzureDataLake> logger, AzureDataLakeStorageOptions options) : base(options)
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
            var result = await _dataLakeServiceClient.CreateFileSystemAsync(StorageOptions.FileSystem, StorageOptions.PublicAccessType, cancellationToken: cancellationToken);
            return Result.Succeeded();
        }
        catch (Exception e)
        {
            _logger.LogError(e.Message,e);
            return Result.Failed(e);
        }
    }

    public override async Task<Result> RemoveContainerAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _dataLakeServiceClient.DeleteFileSystemAsync(StorageOptions.FileSystem, cancellationToken: cancellationToken);
            return Result.Succeeded();
        }
        catch (Exception e)
        {
            _logger.LogError(e.Message,e);
            return Result.Failed(e);
        }
    }

    protected override async Task<Result<string>> UploadInternalAsync(Stream stream, UploadOptions options, CancellationToken cancellationToken = default)
    {
        try
        {
            DataLakeDirectoryClient directoryClient = StorageClient.GetDirectoryClient(options.Directory);
            DataLakeFileClient fileClient = directoryClient.GetFileClient(options.FileName);
            var response = await fileClient.UploadAsync(stream);
            return Result.Succeeded(string.Empty);
        }
        catch (Exception ex)
        {
            return Result<string>.Failed(ex);
        }
    }

    protected override async Task<Result<LocalFile>> DownloadInternalAsync(LocalFile localFile, string blob, CancellationToken cancellationToken = default)
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
            return Result<LocalFile>.Succeeded(localFile);
        }
        catch (Exception ex)
        {
            return Result<LocalFile>.Failed(ex);
        }
        
    }

    public override async Task<Result<bool>> DeleteAsync(string blob, CancellationToken cancellationToken = default)
    {
        try
        {
            DataLakeDirectoryClient directoryClient = StorageClient.GetDirectoryClient(Path.GetDirectoryName(blob));
            DataLakeFileClient fileClient = directoryClient.GetFileClient(Path.GetFileName(blob));
            await fileClient.DeleteAsync(cancellationToken: cancellationToken);
            return Result.Succeeded(true);
        }
        catch (Exception ex)
        {
            return Result<bool>.Failed(ex);
        }
    }

    public override async Task<Result<bool>> ExistsAsync(string blob, CancellationToken cancellationToken = default)
    {
        try
        {
            DataLakeDirectoryClient directoryClient = StorageClient.GetDirectoryClient(Path.GetDirectoryName(blob));
            DataLakeFileClient fileClient = directoryClient.GetFileClient(Path.GetFileName(blob));
            var result = await fileClient.ExistsAsync(cancellationToken: cancellationToken);
            return Result.Succeeded(result.Value);
        }
        catch (Exception ex)
        {
            return Result<bool>.Failed(ex);
        }
    }

    public override async Task<Result<BlobMetadata>> GetBlobMetadataAsync(string blob, CancellationToken cancellationToken = default)
    {
        try
        {
            try
            {
                DataLakeDirectoryClient directoryClient = StorageClient.GetDirectoryClient(Path.GetDirectoryName(blob));
                DataLakeFileClient fileClient = directoryClient.GetFileClient(Path.GetFileName(blob));
                var result = await fileClient.GetPropertiesAsync(cancellationToken: cancellationToken);
                return Result.Succeeded(new BlobMetadata()
                {
                    Name = blob
                });
            }
            catch (Exception ex)
            {
                return Result<BlobMetadata>.Failed(ex);
            }
        }
        catch (Exception ex)
        {
            return Result<BlobMetadata>.Failed(ex);
        }
    }

    public async IAsyncEnumerable<BlobMetadata> GetBlobMetadataListAsync(string directory, CancellationToken cancellationToken = default)
    {
        IAsyncEnumerator<PathItem> enumerator = StorageClient.GetPathsAsync(directory).GetAsyncEnumerator(cancellationToken);
        await enumerator.MoveNextAsync();
        PathItem item = enumerator.Current;
        while (item != null)
        {
            yield return new BlobMetadata
            {
                Name =  item.Name
            };
            
            if (!await enumerator.MoveNextAsync())
            {
                break;
            }
            item = enumerator.Current;
        }
    }

    public override async IAsyncEnumerable<BlobMetadata> GetBlobMetadataListAsync(CancellationToken cancellationToken = default)
    {
        yield return new BlobMetadata();
    }

    public override Task<Result> SetLegalHoldAsync(string blob, bool hasLegalHold, CancellationToken cancellationToken = default)
    {
        return Result.Failed().AsTask();
    }

    public override Task<Result<bool>> HasLegalHoldAsync(string blob, CancellationToken cancellationToken = default)
    {
        return Result<bool>.Failed().AsTask();
    }
    
    public async Task<Result> CreateDirectoryAsync(string directory, CancellationToken cancellationToken = default)
    {
        var result = await StorageClient.CreateDirectoryAsync(directory, cancellationToken: cancellationToken);
        return Result.Succeeded();
    }

    public async Task<Result> RenameDirectory(string directory, string newDirectory, CancellationToken cancellationToken = default)
    {
        DataLakeDirectoryClient directoryClient = StorageClient.GetDirectoryClient(directory);
        var result =  await directoryClient.RenameAsync(newDirectory, cancellationToken: cancellationToken);
        return Result.Succeeded();
    }

    public async Task<Result> DeleteDirectory(string directory, CancellationToken cancellationToken = default)
    {
        var result = await StorageClient.DeleteDirectoryAsync(directory, cancellationToken: cancellationToken);
        return Result.Succeeded();
    }
}