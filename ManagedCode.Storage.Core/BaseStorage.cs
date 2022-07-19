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
        IsContainerCreated = result.IsSuccess;
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

    public Task<Result<string>> UploadAsync(FileInfo file, Action<UploadOptions> action, CancellationToken cancellationToken = default)
    {
        var options = new UploadOptions();
        action.Invoke(options);
        return UploadAsync(file, options, cancellationToken);
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

    protected abstract Task<Result<LocalFile>> DownloadInternalAsync(LocalFile localFile, string blob, DownloadOptions? options = null,
        CancellationToken cancellationToken = default);


    public Task<Result<LocalFile>> DownloadAsync(string blob, CancellationToken cancellationToken = default)
    {
        var file = new LocalFile();
        return DownloadInternalAsync(file, blob, null, cancellationToken);
    }

    public Task<Result<LocalFile>> DownloadAsync(string blob, DownloadOptions options, CancellationToken cancellationToken = default)
    {
        var file = new LocalFile();
        return DownloadInternalAsync(file, blob, options, cancellationToken);
    }

    public Task<Result<LocalFile>> DownloadAsync(string blob, Action<DownloadOptions> action, CancellationToken cancellationToken = default)
    {
        LocalFile file = new();
        DownloadOptions options = new();

        action.Invoke(options);
        return DownloadInternalAsync(file, blob, options, cancellationToken);
    }


    protected abstract Task<Result<bool>> DeleteInternalAsync(string blob, DeleteOptions? options = null,
        CancellationToken cancellationToken = default);

    public Task<Result<bool>> DeleteAsync(string blob, CancellationToken cancellationToken = default)
    {
        return DeleteInternalAsync(blob, null, cancellationToken);
    }

    public Task<Result<bool>> DeleteAsync(string blob, DeleteOptions options, CancellationToken cancellationToken = default)
    {
        return DeleteInternalAsync(blob, options, cancellationToken);
    }

    public Task<Result<bool>> DeleteAsync(string blob, Action<DeleteOptions> action, CancellationToken cancellationToken = default)
    {
        DeleteOptions options = new();
        action.Invoke(options);
        return DeleteInternalAsync(blob, options, cancellationToken);
    }

    protected abstract Task<Result<bool>> ExistsInternalAsync(string blob, ExistOptions? options = null,
        CancellationToken cancellationToken = default);

    public Task<Result<bool>> ExistsAsync(string blob, CancellationToken cancellationToken = default)
    {
        return ExistsInternalAsync(blob, null, cancellationToken);
    }

    public Task<Result<bool>> ExistsAsync(string blob, ExistOptions options, CancellationToken cancellationToken = default)
    {
        return ExistsInternalAsync(blob, options, cancellationToken);
    }

    public Task<Result<bool>> ExistsAsync(string blob, Action<ExistOptions> action, CancellationToken cancellationToken = default)
    {
        ExistOptions options = new();
        action.Invoke(options);
        return ExistsInternalAsync(blob, options, cancellationToken);
    }

    protected abstract Task<Result<BlobMetadata>> GetBlobMetadataInternalAsync(string blob, MetadataOptions? options = null,
        CancellationToken cancellationToken = default);

    public Task<Result<BlobMetadata>> GetBlobMetadataAsync(string blob, CancellationToken cancellationToken = default)
    {
        return GetBlobMetadataInternalAsync(blob, null, cancellationToken);
    }

    public Task<Result<BlobMetadata>> GetBlobMetadataAsync(string blob, MetadataOptions options, CancellationToken cancellationToken = default)
    {
        return GetBlobMetadataInternalAsync(blob, options, cancellationToken);
    }

    public Task<Result<BlobMetadata>> GetBlobMetadataAsync(string blob, Action<MetadataOptions> action,
        CancellationToken cancellationToken = default)
    {
        MetadataOptions options = new();
        action.Invoke(options);
        return GetBlobMetadataInternalAsync(blob, options, cancellationToken);
    }

    public abstract IAsyncEnumerable<BlobMetadata> GetBlobMetadataListAsync(CancellationToken cancellationToken = default);

    public abstract Task<Result> SetLegalHoldAsync(string blob, bool hasLegalHold, CancellationToken cancellationToken = default);

    public abstract Task<Result<bool>> HasLegalHoldAsync(string blob, CancellationToken cancellationToken = default);
}