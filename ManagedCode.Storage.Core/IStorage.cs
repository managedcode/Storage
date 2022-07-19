using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using ManagedCode.Communication;
using ManagedCode.Storage.Core.Models;

namespace ManagedCode.Storage.Core;

public interface IStorage<out T> : IStorage
{
    T StorageClient { get; }
}

public interface IStorage
{
    Task<Result> CreateContainerAsync(CancellationToken cancellationToken = default);
    Task<Result> RemoveContainerAsync(CancellationToken cancellationToken = default);


    Task<Result<string>> UploadAsync(Stream stream, CancellationToken cancellationToken = default);
    Task<Result<string>> UploadAsync(byte[] data, CancellationToken cancellationToken = default);
    Task<Result<string>> UploadAsync(string content, CancellationToken cancellationToken = default);
    Task<Result<string>> UploadAsync(FileInfo file, CancellationToken cancellationToken = default);

    Task<Result<string>> UploadAsync(Stream stream, UploadOptions options, CancellationToken cancellationToken = default);
    Task<Result<string>> UploadAsync(byte[] data, UploadOptions options, CancellationToken cancellationToken = default);
    Task<Result<string>> UploadAsync(string content, UploadOptions options, CancellationToken cancellationToken = default);
    Task<Result<string>> UploadAsync(FileInfo file, UploadOptions options, CancellationToken cancellationToken = default);

    Task<Result<string>> UploadAsync(Stream stream, Action<UploadOptions> action, CancellationToken cancellationToken = default);
    Task<Result<string>> UploadAsync(byte[] data, Action<UploadOptions> action, CancellationToken cancellationToken = default);
    Task<Result<string>> UploadAsync(string content, Action<UploadOptions> action, CancellationToken cancellationToken = default);
    Task<Result<string>> UploadAsync(FileInfo file, Action<UploadOptions> action, CancellationToken cancellationToken = default);


    Task<Result<LocalFile>> DownloadAsync(string blob, CancellationToken cancellationToken = default);
    Task<Result<LocalFile>> DownloadAsync(string blob, DownloadOptions options, CancellationToken cancellationToken = default);
    Task<Result<LocalFile>> DownloadAsync(string blob, Action<DownloadOptions> action, CancellationToken cancellationToken = default);


    Task<Result<bool>> DeleteAsync(string blob, CancellationToken cancellationToken = default);
    Task<Result<bool>> DeleteAsync(string blob, DeleteOptions options, CancellationToken cancellationToken = default);
    Task<Result<bool>> DeleteAsync(string blob, Action<DeleteOptions> action, CancellationToken cancellationToken = default);


    Task<Result<bool>> ExistsAsync(string blob, CancellationToken cancellationToken = default);
    Task<Result<bool>> ExistsAsync(string blob, ExistOptions options, CancellationToken cancellationToken = default);
    Task<Result<bool>> ExistsAsync(string blob, Action<ExistOptions> action, CancellationToken cancellationToken = default);


    Task<Result<BlobMetadata>> GetBlobMetadataAsync(string blob, CancellationToken cancellationToken = default);
    Task<Result<BlobMetadata>> GetBlobMetadataAsync(string blob, MetadataOptions options, CancellationToken cancellationToken = default);
    Task<Result<BlobMetadata>> GetBlobMetadataAsync(string blob, Action<MetadataOptions> action, CancellationToken cancellationToken = default);

    // TODO: Check and add overloads with options
    IAsyncEnumerable<BlobMetadata> GetBlobMetadataListAsync(CancellationToken cancellationToken = default); //???????


    // TODO: Add overloads with options
    Task<Result> SetLegalHoldAsync(string blob, bool hasLegalHold, CancellationToken cancellationToken = default);
    Task<Result<bool>> HasLegalHoldAsync(string blob, CancellationToken cancellationToken = default);
}