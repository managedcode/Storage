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


    Task<Result> DeleteDirectoryAsync(string directory, CancellationToken cancellationToken = default);


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
    Task<Result<LocalFile>> DownloadAsync(DownloadOptions options, CancellationToken cancellationToken = default);
    Task<Result<LocalFile>> DownloadAsync(Action<DownloadOptions> action, CancellationToken cancellationToken = default);


    Task<Result<bool>> DeleteAsync(string blob, CancellationToken cancellationToken = default);
    Task<Result<bool>> DeleteAsync(DeleteOptions options, CancellationToken cancellationToken = default);
    Task<Result<bool>> DeleteAsync(Action<DeleteOptions> action, CancellationToken cancellationToken = default);


    Task<Result<bool>> ExistsAsync(string blob, CancellationToken cancellationToken = default);
    Task<Result<bool>> ExistsAsync(ExistOptions options, CancellationToken cancellationToken = default);
    Task<Result<bool>> ExistsAsync(Action<ExistOptions> action, CancellationToken cancellationToken = default);


    Task<Result<BlobMetadata>> GetBlobMetadataAsync(string blob, CancellationToken cancellationToken = default);
    Task<Result<BlobMetadata>> GetBlobMetadataAsync(MetadataOptions options, CancellationToken cancellationToken = default);
    Task<Result<BlobMetadata>> GetBlobMetadataAsync(Action<MetadataOptions> action, CancellationToken cancellationToken = default);

    IAsyncEnumerable<BlobMetadata> GetBlobMetadataListAsync(string? directory = null, CancellationToken cancellationToken = default);

    Task<Result> SetLegalHoldAsync(bool hasLegalHold, string blob, CancellationToken cancellationToken = default);
    Task<Result> SetLegalHoldAsync(bool hasLegalHold, LegalHoldOptions options, CancellationToken cancellationToken = default);
    Task<Result> SetLegalHoldAsync(bool hasLegalHold, Action<LegalHoldOptions> action, CancellationToken cancellationToken = default);

    Task<Result<bool>> HasLegalHoldAsync(string blob, CancellationToken cancellationToken = default);
    Task<Result<bool>> HasLegalHoldAsync(LegalHoldOptions options, CancellationToken cancellationToken = default);
    Task<Result<bool>> HasLegalHoldAsync(Action<LegalHoldOptions> action, CancellationToken cancellationToken = default);
}