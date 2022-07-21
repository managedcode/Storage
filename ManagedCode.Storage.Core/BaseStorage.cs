using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using ManagedCode.Communication;
using ManagedCode.MimeTypes;
using ManagedCode.Storage.Core.Models;

namespace ManagedCode.Storage.Core;

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
        StorageOptions = storageOptions!;
    }

    protected Task<Result> EnsureContainerExist()
    {
        if (IsContainerCreated)
            return Result.Succeed().AsTask();

        return CreateContainerAsync();
    }

    protected UploadOptions SetUploadOptions(UploadOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.FileName))
            options.FileName = $"{Guid.NewGuid():N}";

        if (!string.IsNullOrWhiteSpace(options.FileNamePrefix))
            options.FileName = options.FileNamePrefix + options.FileName;

        return options;
    }

    public async Task<Result> CreateContainerAsync(CancellationToken cancellationToken = default)
    {
        var result = await CreateContainerInternalAsync(cancellationToken);
        cancellationToken.ThrowIfCancellationRequested();
        IsContainerCreated = result.IsSuccess;
        return result;
    }

    protected abstract Task<Result> CreateContainerInternalAsync(CancellationToken cancellationToken = default);
    public abstract Task<Result> RemoveContainerAsync(CancellationToken cancellationToken = default);

    protected abstract Task<Result> DeleteDirectoryInternalAsync(string directory, CancellationToken cancellationToken = default);

    public async Task<Result> DeleteDirectoryAsync(string directory, CancellationToken cancellationToken = default)
    {
        return await DeleteDirectoryInternalAsync(directory, cancellationToken);
    }

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

    public Task<Result<string>> UploadAsync(FileInfo fileInfo, CancellationToken cancellationToken = default)
    {
        return UploadAsync(fileInfo, new UploadOptions(), cancellationToken);
    }

    public Task<Result<string>> UploadAsync(Stream stream, Action<UploadOptions> action, CancellationToken cancellationToken = default)
    {
        var options = new UploadOptions();
        action.Invoke(options);
        return UploadAsync(stream, options, cancellationToken);
    }

    public Task<Result<string>> UploadAsync(byte[] data, Action<UploadOptions> action, CancellationToken cancellationToken = default)
    {
        var options = new UploadOptions();
        action.Invoke(options);
        return UploadAsync(data, options, cancellationToken);
    }

    public Task<Result<string>> UploadAsync(string content, Action<UploadOptions> action, CancellationToken cancellationToken = default)
    {
        var options = new UploadOptions();
        action.Invoke(options);
        return UploadAsync(content, options, cancellationToken);
    }

    public Task<Result<string>> UploadAsync(FileInfo fileInfo, Action<UploadOptions> action, CancellationToken cancellationToken = default)
    {
        var options = new UploadOptions();
        action.Invoke(options);
        return UploadAsync(fileInfo, options, cancellationToken);
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

    public Task<Result<string>> UploadAsync(FileInfo fileInfo, UploadOptions options, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(options.MimeType))
        {
            options.MimeType = MimeHelper.GetMimeType(fileInfo.Extension);
        }

        return UploadInternalAsync(fileInfo.OpenRead(), SetUploadOptions(options), cancellationToken);
    }

    protected abstract Task<Result<LocalFile>> DownloadInternalAsync(LocalFile localFile, DownloadOptions options,
        CancellationToken cancellationToken = default);


    public Task<Result<LocalFile>> DownloadAsync(string fileName, CancellationToken cancellationToken = default)
    {
        var file = new LocalFile();
        DownloadOptions options = new() {FileName = fileName};
        return DownloadInternalAsync(file, options, cancellationToken);
    }

    public Task<Result<LocalFile>> DownloadAsync(DownloadOptions options, CancellationToken cancellationToken = default)
    {
        var keepAlive = options.LocalPath is not null;
        LocalFile file = new(options.LocalPath, keepAlive);
        
        return DownloadInternalAsync(file, options, cancellationToken);
    }

    public Task<Result<LocalFile>> DownloadAsync(Action<DownloadOptions> action, CancellationToken cancellationToken = default)
    {
        DownloadOptions options = new();
        action.Invoke(options);

        var keepAlive = options.LocalPath is not null;
        LocalFile file = new(options.LocalPath, keepAlive);
        return DownloadInternalAsync(file, options, cancellationToken);
    }
    
    protected abstract Task<Result<bool>> DeleteInternalAsync(DeleteOptions options, CancellationToken cancellationToken = default);

    public Task<Result<bool>> DeleteAsync(string fileName, CancellationToken cancellationToken = default)
    {
        DeleteOptions options = new() {FileName = fileName};
        return DeleteInternalAsync(options, cancellationToken);
    }

    public Task<Result<bool>> DeleteAsync(DeleteOptions options, CancellationToken cancellationToken = default)
    {
        return DeleteInternalAsync(options, cancellationToken);
    }

    public Task<Result<bool>> DeleteAsync(Action<DeleteOptions> action, CancellationToken cancellationToken = default)
    {
        DeleteOptions options = new();
        action.Invoke(options);
        return DeleteInternalAsync(options, cancellationToken);
    }

    protected abstract Task<Result<bool>> ExistsInternalAsync(ExistOptions options, CancellationToken cancellationToken = default);

    public Task<Result<bool>> ExistsAsync(string fileName, CancellationToken cancellationToken = default)
    {
        ExistOptions options = new() {FileName = fileName};
        return ExistsInternalAsync(options, cancellationToken);
    }

    public Task<Result<bool>> ExistsAsync(ExistOptions options, CancellationToken cancellationToken = default)
    {
        return ExistsInternalAsync(options, cancellationToken);
    }

    public Task<Result<bool>> ExistsAsync(Action<ExistOptions> action, CancellationToken cancellationToken = default)
    {
        ExistOptions options = new();
        action.Invoke(options);
        return ExistsInternalAsync(options, cancellationToken);
    }

    protected abstract Task<Result<BlobMetadata>> GetBlobMetadataInternalAsync(MetadataOptions options,
        CancellationToken cancellationToken = default);

    public Task<Result<BlobMetadata>> GetBlobMetadataAsync(string fileName, CancellationToken cancellationToken = default)
    {
        MetadataOptions options = new() {FileName = fileName};
        return GetBlobMetadataInternalAsync(options, cancellationToken);
    }

    public Task<Result<BlobMetadata>> GetBlobMetadataAsync(MetadataOptions options, CancellationToken cancellationToken = default)
    {
        return GetBlobMetadataInternalAsync(options, cancellationToken);
    }

    public Task<Result<BlobMetadata>> GetBlobMetadataAsync(Action<MetadataOptions> action, CancellationToken cancellationToken = default)
    {
        MetadataOptions options = new();
        action.Invoke(options);
        return GetBlobMetadataInternalAsync(options, cancellationToken);
    }

    public abstract IAsyncEnumerable<BlobMetadata> GetBlobMetadataListAsync(string? directory = null, CancellationToken cancellationToken = default);


    protected abstract Task<Result> SetLegalHoldInternalAsync(bool hasLegalHold, LegalHoldOptions options,
        CancellationToken cancellationToken = default);

    public Task<Result> SetLegalHoldAsync(bool hasLegalHold, string fileName, CancellationToken cancellationToken = default)
    {
        LegalHoldOptions options = new() {FileName = fileName};
        return SetLegalHoldInternalAsync(hasLegalHold, options, cancellationToken);
    }

    public Task<Result> SetLegalHoldAsync(bool hasLegalHold, LegalHoldOptions options, CancellationToken cancellationToken = default)
    {
        return SetLegalHoldInternalAsync(hasLegalHold, options, cancellationToken);
    }

    public Task<Result> SetLegalHoldAsync(bool hasLegalHold, Action<LegalHoldOptions> action, CancellationToken cancellationToken = default)
    {
        LegalHoldOptions options = new();
        action.Invoke(options);
        return SetLegalHoldInternalAsync(hasLegalHold, options, cancellationToken);
    }

    protected abstract Task<Result<bool>> HasLegalHoldInternalAsync(LegalHoldOptions options, CancellationToken cancellationToken = default);

    public Task<Result<bool>> HasLegalHoldAsync(string fileName, CancellationToken cancellationToken = default)
    {
        LegalHoldOptions options = new() {FileName = fileName};
        return HasLegalHoldInternalAsync(options, cancellationToken);
    }

    public Task<Result<bool>> HasLegalHoldAsync(LegalHoldOptions options, CancellationToken cancellationToken = default)
    {
        return HasLegalHoldInternalAsync(options, cancellationToken);
    }

    public Task<Result<bool>> HasLegalHoldAsync(Action<LegalHoldOptions> action, CancellationToken cancellationToken = default)
    {
        LegalHoldOptions options = new();
        action.Invoke(options);
        return HasLegalHoldInternalAsync(options, cancellationToken);
    }
}