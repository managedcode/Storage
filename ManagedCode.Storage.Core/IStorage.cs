using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using ManagedCode.Communication;
using ManagedCode.Storage.Core.Models;

namespace ManagedCode.Storage.Core;

public interface IStorage<out T, TOptions> : IStorage where TOptions : StorageOptions
{
    T StorageClient { get; }
    Task<Result> SetStorageOptions(TOptions options, CancellationToken cancellationToken = default);
    Task<Result> SetStorageOptions(Action<TOptions> options, CancellationToken cancellationToken = default);
}

public interface IStorage
{
    /// <summary>
    ///     Create a container if it does not already exist.
    /// </summary>
    Task<Result> CreateContainerAsync(CancellationToken cancellationToken = default);

    /// <summary>
    ///     Delete a container if it does not already exist.
    /// </summary>
    Task<Result> RemoveContainerAsync(CancellationToken cancellationToken = default);

    /// <summary>
    ///     Delete the folder along with its contents
    /// </summary>
    Task<Result> DeleteDirectoryAsync(string directory, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Upload data from the stream into the blob storage.
    /// </summary>
    Task<Result<BlobMetadata>> UploadAsync(Stream stream, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Upload array of bytes into the blob storage.
    /// </summary>
    Task<Result<BlobMetadata>> UploadAsync(byte[] data, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Upload data from the string into the blob storage.
    /// </summary>
    Task<Result<BlobMetadata>> UploadAsync(string content, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Upload data from the file into the blob storage.
    /// </summary>
    Task<Result<BlobMetadata>> UploadAsync(FileInfo fileInfo, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Upload data from the stream into the blob storage.
    /// </summary>
    Task<Result<BlobMetadata>> UploadAsync(Stream stream, UploadOptions options, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Upload array of bytes into the blob storage.
    /// </summary>
    Task<Result<BlobMetadata>> UploadAsync(byte[] data, UploadOptions options, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Upload data from the string into the blob storage.
    /// </summary>
    Task<Result<BlobMetadata>> UploadAsync(string content, UploadOptions options, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Upload data from the file into the blob storage.
    /// </summary>
    Task<Result<BlobMetadata>> UploadAsync(FileInfo fileInfo, UploadOptions options, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Upload data from the stream into the blob storage.
    /// </summary>
    Task<Result<BlobMetadata>> UploadAsync(Stream stream, Action<UploadOptions> action, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Upload array of bytes into the blob storage.
    /// </summary>
    Task<Result<BlobMetadata>> UploadAsync(byte[] data, Action<UploadOptions> action, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Upload data from the string into the blob storage.
    /// </summary>
    Task<Result<BlobMetadata>> UploadAsync(string content, Action<UploadOptions> action, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Upload data from the file into the blob storage.
    /// </summary>
    Task<Result<BlobMetadata>> UploadAsync(FileInfo fileInfo, Action<UploadOptions> action, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Downloads and saves the file to the local file system.
    /// </summary>
    Task<Result<LocalFile>> DownloadAsync(string fileName, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Downloads and saves the file to the local file system.
    /// </summary>
    Task<Result<LocalFile>> DownloadAsync(DownloadOptions options, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Downloads and saves the file to the local file system.
    /// </summary>
    Task<Result<LocalFile>> DownloadAsync(Action<DownloadOptions> action, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Deletes a file from the blob storage
    /// </summary>
    Task<Result<bool>> DeleteAsync(string fileName, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Deletes a file from the blob storage
    /// </summary>
    Task<Result<bool>> DeleteAsync(DeleteOptions options, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Deletes a file from the blob storage
    /// </summary>
    Task<Result<bool>> DeleteAsync(Action<DeleteOptions> action, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Checking if a file exists in the blob storage
    /// </summary>
    Task<Result<bool>> ExistsAsync(string fileName, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Checking if a file exists in the blob storage
    /// </summary>
    Task<Result<bool>> ExistsAsync(ExistOptions options, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Checking if a file exists in the blob storage
    /// </summary>
    Task<Result<bool>> ExistsAsync(Action<ExistOptions> action, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Returns the metadata of the file from the blob storage
    /// </summary>
    Task<Result<BlobMetadata>> GetBlobMetadataAsync(string fileName, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Returns the metadata of the file from the blob storage
    /// </summary>
    Task<Result<BlobMetadata>> GetBlobMetadataAsync(MetadataOptions options, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Returns the metadata of the file from the blob storage
    /// </summary>
    Task<Result<BlobMetadata>> GetBlobMetadataAsync(Action<MetadataOptions> action, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Returns metadata of all files in the specified path from the blob storage
    /// </summary>
    IAsyncEnumerable<BlobMetadata> GetBlobMetadataListAsync(string? directory = null, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Set LegalHold
    /// </summary>
    Task<Result> SetLegalHoldAsync(bool hasLegalHold, string fileName, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Set LegalHold
    /// </summary>
    Task<Result> SetLegalHoldAsync(bool hasLegalHold, LegalHoldOptions options, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Set LegalHold
    /// </summary>
    Task<Result> SetLegalHoldAsync(bool hasLegalHold, Action<LegalHoldOptions> action, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Check LegalHold
    /// </summary>
    Task<Result<bool>> HasLegalHoldAsync(string fileName, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Check LegalHold
    /// </summary
    Task<Result<bool>> HasLegalHoldAsync(LegalHoldOptions options, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Check LegalHold
    /// </summary
    Task<Result<bool>> HasLegalHoldAsync(Action<LegalHoldOptions> action, CancellationToken cancellationToken = default);
}