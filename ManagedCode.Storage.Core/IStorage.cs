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


    Task<Result<string>> UploadAsync(Stream stream, Action<UploadOptions> options, CancellationToken cancellationToken = default);
    Task<Result<string>> UploadAsync(byte[] data, Action<UploadOptions> options, CancellationToken cancellationToken = default);
    Task<Result<string>> UploadAsync(string content, Action<UploadOptions> options, CancellationToken cancellationToken = default);
    Task<Result<string>> UploadAsync(FileInfo file, Action<UploadOptions> options, CancellationToken cancellationToken = default);


    Task<Result<string>> UploadAsync(Stream stream, UploadOptions options, CancellationToken cancellationToken = default);
    Task<Result<string>> UploadAsync(byte[] data, UploadOptions options, CancellationToken cancellationToken = default);
    Task<Result<string>> UploadAsync(string content, UploadOptions options, CancellationToken cancellationToken = default);
    Task<Result<string>> UploadAsync(FileInfo file, UploadOptions options, CancellationToken cancellationToken = default);


    Task<Result<LocalFile>> DownloadAsync(string blob, CancellationToken cancellationToken = default);
    Task<Result<LocalFile>> DownloadToAsync(string blob, string localPath, CancellationToken cancellationToken = default);

    Task<Result<bool>> DeleteAsync(string blob, CancellationToken cancellationToken = default);


    Task<Result<bool>> ExistsAsync(string blob, CancellationToken cancellationToken = default);
    Task<Result<BlobMetadata>> GetBlobMetadataAsync(string blob, CancellationToken cancellationToken = default);
    IAsyncEnumerable<BlobMetadata> GetBlobMetadataListAsync(CancellationToken cancellationToken = default); //???????


    Task<Result> SetLegalHoldAsync(string blob, bool hasLegalHold, CancellationToken cancellationToken = default);
    Task<Result<bool>> HasLegalHoldAsync(string blob, CancellationToken cancellationToken = default);
}