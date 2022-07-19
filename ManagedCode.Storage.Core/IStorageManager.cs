using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using ManagedCode.Communication;
using ManagedCode.MimeTypes;
using ManagedCode.Storage.Core.Models;

namespace ManagedCode.Storage.Core;

public interface IDataLakeStorage<T> : IStorage<T>
{
    Task<Result<Stream>> OpenReadStreamAsync(string blob, CancellationToken cancellationToken = default);
    Task<Result<Stream>> OpenWriteStreamAsync(string blob, CancellationToken cancellationToken = default);

}


public interface IStorage<out T> : IStorage
{
    T StorageClient { get; }
}

public class StorageOptions
{
    public bool CreateContainerIfNotExists { get; set; } = true;
}

public abstract class BaseStorage<T> : IStorage where T : StorageOptions
{
    protected bool IsContainerCreated;
    protected readonly T StorageOptions;

    protected BaseStorage(T storageOptions)
    {
        System.Diagnostics.Contracts.Contract.Assert(storageOptions is not null);
        
        StorageOptions = storageOptions;
    }

    protected Task<Result> EnsureContainerExist()
    {
        if (IsContainerCreated)
            return Result.Succeeded().AsTask();
            
        return CreateContainerAsync();
    }
    
    protected UploadOptions SetUploadOptions(UploadOptions options)
    {
        if(string.IsNullOrWhiteSpace(options.FileName))
            options.FileName = Guid.NewGuid().ToString("N");

        if (!string.IsNullOrWhiteSpace(options.FileNamePrefix))
            options.FileName = options.FileNamePrefix + options.FileName;
        
        if (!string.IsNullOrWhiteSpace(options.Directory))
            options.FileName = new Uri(Path.Combine(options.Directory, options.FileName)).ToString();
        
        return options;
    }
    
    public async Task<Result> CreateContainerAsync(CancellationToken cancellationToken = default)
    {
        var result = await CreateContainerInternalAsync(cancellationToken);
        cancellationToken.ThrowIfCancellationRequested();
        IsContainerCreated = result.IsSucceeded;
        return result;
    }

    protected abstract Task<Result> CreateContainerInternalAsync(CancellationToken cancellationToken = default);
    public abstract Task<Result> RemoveContainerAsync(CancellationToken cancellationToken = default);
    protected abstract Task<Result<string>> UploadInternalAsync(Stream stream, UploadOptions options, CancellationToken cancellationToken = default);
    
    public Task<Result<string>> UploadAsync(Stream content, CancellationToken cancellationToken = default)
    {
        return UploadAsync(content, new UploadOptions(), cancellationToken);
    }
    
    public Task<Result<string>> UploadAsync(byte[] data, CancellationToken cancellationToken = default)
    {
        return UploadAsync(data, new UploadOptions(), cancellationToken);
    }

    public Task<Result<string>> UploadAsync(string content, CancellationToken cancellationToken = default)
    {
        return UploadAsync(content, new UploadOptions(), cancellationToken);
    }

    public Task<Result<string>> UploadAsync(FileInfo file, CancellationToken cancellationToken = default)
    {
        return UploadAsync(file, new UploadOptions(), cancellationToken);
    }

    public Task<Result<string>> UploadAsync(Stream stream, Action<UploadOptions> options, CancellationToken cancellationToken = default)
    {
        var optionsInstance = new UploadOptions();
        options.Invoke(optionsInstance);
        return UploadAsync(stream, optionsInstance, cancellationToken);
    }

    public Task<Result<string>> UploadAsync(byte[] data, Action<UploadOptions> options, CancellationToken cancellationToken = default)
    {
        var optionsInstance = new UploadOptions();
        options.Invoke(optionsInstance);
        return UploadAsync(data, optionsInstance, cancellationToken);
    }

    public Task<Result<string>> UploadAsync(string content, Action<UploadOptions> options, CancellationToken cancellationToken = default)
    {
        var optionsInstance = new UploadOptions();
        options.Invoke(optionsInstance);
        return UploadAsync(content, optionsInstance, cancellationToken);
    }

    public Task<Result<string>> UploadAsync(FileInfo file, Action<UploadOptions> options, CancellationToken cancellationToken = default)
    {
        var optionsInstance = new UploadOptions();
        options.Invoke(optionsInstance);
        return UploadAsync(file, optionsInstance, cancellationToken);
    }
    
    public Task<Result<string>> UploadAsync(Stream stream, UploadOptions options, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(options.MimeType))
        {
            options.MimeType = MimeHelper.BIN;
        }
        
        return UploadInternalAsync(stream, SetUploadOptions(options), cancellationToken);
    }
    
    public Task<Result<string>> UploadAsync(byte[] data, UploadOptions options, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(options.MimeType))
        {
            options.MimeType = MimeHelper.BIN;
        }
        
        return UploadInternalAsync(new MemoryStream(data), SetUploadOptions(options), cancellationToken);
    }

    public Task<Result<string>> UploadAsync(string content, UploadOptions options, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(options.MimeType))
        {
            options.MimeType = MimeHelper.TEXT;
        }
        
        return UploadInternalAsync(new StringStream(content), SetUploadOptions(options), cancellationToken);
    }

    public Task<Result<string>> UploadAsync(FileInfo file, UploadOptions options, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(options.MimeType))
        {
            options.MimeType = MimeHelper.GetMimeType(file.Extension);
        }

        return UploadInternalAsync(file.OpenRead(), SetUploadOptions(options), cancellationToken);
    }

    protected abstract  Task<Result<LocalFile>> DownloadInternalAsync(LocalFile localFile, string blob, CancellationToken cancellationToken = default);
        
    
    public Task<Result<LocalFile>> DownloadAsync(string blob, CancellationToken cancellationToken = default)
    {
        var file = new LocalFile();
        return DownloadInternalAsync(file, blob, cancellationToken);
    }

    public Task<Result<LocalFile>> DownloadToAsync(string blob, string localPath, CancellationToken cancellationToken = default)
    {
        var file = new LocalFile(localPath);
        return DownloadInternalAsync(file, blob, cancellationToken);
    }

    public Task<Result<LocalFile>> DownloadAsync(BlobMetadata blob, CancellationToken cancellationToken = default)
    {
        return DownloadAsync(blob.Name, cancellationToken);
    }

    public Task<Result<LocalFile>> DownloadToAsync(BlobMetadata blob, string localPath, CancellationToken cancellationToken = default)
    {
        return DownloadToAsync(blob.Name, localPath, cancellationToken);
    }

    public abstract Task<Result<bool>> DeleteAsync(string blob, CancellationToken cancellationToken = default);

    public abstract Task<Result<bool>> ExistsAsync(string blob, CancellationToken cancellationToken = default);

    public abstract Task<Result<BlobMetadata>> GetBlobMetadataAsync(string blob, CancellationToken cancellationToken = default);

    public abstract IAsyncEnumerable<BlobMetadata> GetBlobMetadataListAsync(CancellationToken cancellationToken = default);

    public abstract Task<Result> SetLegalHoldAsync(string blob, bool hasLegalHold, CancellationToken cancellationToken = default);

    public abstract Task<Result<bool>> HasLegalHoldAsync(string blob, CancellationToken cancellationToken = default);
}

public interface IStorage
{
    Task<Result> CreateContainerAsync(CancellationToken cancellationToken = default);
    Task<Result> RemoveContainerAsync(CancellationToken cancellationToken = default);
    
    
    Task<Result<string>> UploadAsync(Stream stream, CancellationToken cancellationToken = default);
    Task<Result<string>> UploadAsync(byte[] data, CancellationToken cancellationToken = default);
    Task<Result<string>> UploadAsync(string content, CancellationToken cancellationToken = default);
    Task<Result<string>> UploadAsync(FileInfo file, CancellationToken cancellationToken = default);

    
    Task<Result<string>> UploadAsync(Stream stream, Action<UploadOptions> options, CancellationToken cancellationToken = default);
    Task<Result<string>> UploadAsync(byte[] data, Action<UploadOptions> options, CancellationToken cancellationToken = default);
    Task<Result<string>> UploadAsync(string content, Action<UploadOptions> options, CancellationToken cancellationToken = default);
    Task<Result<string>> UploadAsync(FileInfo file, Action<UploadOptions> options, CancellationToken cancellationToken = default);
    
    
    Task<Result<string>> UploadAsync(Stream stream, UploadOptions options, CancellationToken cancellationToken = default);
    Task<Result<string>> UploadAsync(byte[] data, UploadOptions options, CancellationToken cancellationToken = default);
    Task<Result<string>> UploadAsync(string content, UploadOptions options, CancellationToken cancellationToken = default);
    Task<Result<string>> UploadAsync(FileInfo file,  UploadOptions options, CancellationToken cancellationToken = default);


    Task<Result<LocalFile>> DownloadAsync(string blob, CancellationToken cancellationToken = default);
    Task<Result<LocalFile>> DownloadToAsync(string blob, string localPath, CancellationToken cancellationToken = default);
    Task<Result<LocalFile>> DownloadAsync(BlobMetadata blob, CancellationToken cancellationToken = default);
    Task<Result<LocalFile>> DownloadToAsync(BlobMetadata blob, string localPath, CancellationToken cancellationToken = default);
    
    Task<Result<bool>> DeleteAsync(string blob, CancellationToken cancellationToken = default);
    
    
    Task<Result<bool>> ExistsAsync(string blob, CancellationToken cancellationToken = default);
    Task<Result<BlobMetadata>> GetBlobMetadataAsync(string blob, CancellationToken cancellationToken = default);
    IAsyncEnumerable<BlobMetadata> GetBlobMetadataListAsync(CancellationToken cancellationToken = default); //???????


    Task<Result> SetLegalHoldAsync(string blob, bool hasLegalHold, CancellationToken cancellationToken = default);
    Task<Result<bool>> HasLegalHoldAsync(string blob, CancellationToken cancellationToken = default);
    
    
}




public class UploadOptions
{
    public UploadOptions()
    {
        
    }
    public UploadOptions(string? fileName = null, string? directory = null, string? mimeType = null, Dictionary<string, string>? metadata = null, string? fileNamePrefix = null)
    {
        FileName = fileName;
        MimeType = mimeType;
        Directory = directory;
        Metadata = metadata;
        FileNamePrefix = fileNamePrefix;
    }
    
    public string? FileName { get; set; }
    public string? FileNamePrefix { get; set; }
    public string? Directory { get; set; }
    public string? MimeType  { get; set; }
    public Dictionary<string,string>? Metadata { get; set; }
}

