// using System;
// using System.Collections.Generic;
// using System.IO;
// using System.Threading;
// using System.Threading.Tasks;
// using ManagedCode.Communication;
// using ManagedCode.Storage.Core.Models;
//
// namespace ManageCode.FileStream.Client.Abstractions;
// public interface IFileEndpoint
// {
//
//
//     public Task UploadAsync(string[] filesPath);
// }
//
// public class Prog
// {
//     public void Do()
//     {
//         IFileClient client;
//
//         var a = client.UploadAsync(x => x.FromPath("file path"));
//     }
// }
//
// public class FileUploadExtensions
// {
//
//     /// <summary>
//     ///     Upload data from the stream into the blob storage.
//     /// </summary>
//     Task<Result<BlobMetadata>> UploadAsync(Stream stream, CancellationToken cancellationToken = default);
//
//     /// <summary>
//     ///     Upload array of bytes into the blob storage.
//     /// </summary>
//     Task<Result<BlobMetadata>> UploadAsync(byte[] data, CancellationToken cancellationToken = default);
//
//     /// <summary>
//     ///     Upload data from the string into the blob storage.
//     /// </summary>
//     Task<Result<BlobMetadata>> UploadAsync(string content, CancellationToken cancellationToken = default);
//
//     /// <summary>
//     ///     Upload data from the file into the blob storage.
//     /// </summary>
//     Task<Result<BlobMetadata>> UploadAsync(FileInfo fileInfo, CancellationToken cancellationToken = default);
//
//     /// <summary>
//     ///     Upload data from the stream into the blob storage.
//     /// </summary>
//     Task<Result<BlobMetadata>> UploadAsync(Stream stream, UploadOptions options, CancellationToken cancellationToken = default);
//
//     /// <summary>
//     ///     Upload array of bytes into the blob storage.
//     /// </summary>
//     Task<Result<BlobMetadata>> UploadAsync(byte[] data, UploadOptions options, CancellationToken cancellationToken = default);
//
//     /// <summary>
//     ///     Upload data from the string into the blob storage.
//     /// </summary>
//     Task<Result<BlobMetadata>> UploadAsync(string content, UploadOptions options, CancellationToken cancellationToken = default);
//
//     /// <summary>
//     ///     Upload data from the file into the blob storage.
//     /// </summary>
//     Task<Result<BlobMetadata>> UploadAsync(FileInfo fileInfo, UploadOptions options, CancellationToken cancellationToken = default);
//
//     /// <summary>
//     ///     Upload data from the stream into the blob storage.
//     /// </summary>
//     Task<Result<BlobMetadata>> UploadAsync(Stream stream, Action<UploadOptions> action, CancellationToken cancellationToken = default);
//
//     /// <summary>
//     ///     Upload array of bytes into the blob storage.
//     /// </summary>
//     Task<Result<BlobMetadata>> UploadAsync(byte[] data, Action<UploadOptions> action, CancellationToken cancellationToken = default);
//
//     /// <summary>
//     ///     Upload data from the string into the blob storage.
//     /// </summary>
//     Task<Result<BlobMetadata>> UploadAsync(string content, Action<UploadOptions> action, CancellationToken cancellationToken = default);
//
//     /// <summary>
//     ///     Upload data from the file into the blob storage.
//     /// </summary>
//     Task<Result<BlobMetadata>> UploadAsync(FileInfo fileInfo, Action<UploadOptions> action, CancellationToken cancellationToken = default);
// }
//
// public class BlobMetaData
// {
//
// }
//
//
// public interface IBlobStorage<TStorage> :
//     IFileUploader<BlobMetaData, UploadOptions>,
//     IFileDownloader<BlobMetaData, DownloadOptions>,
//     IFileDeleter<object, object>,
//     ILegalHold,
//     IMetaDataReader<object>, IStorageOptions<TStorage>
//
// {
//     public Task<bool> IsFileExistsAsync(Action<IFileChooser> file);
//
//     Task CreateContainerAsync(CancellationToken cancellationToken = default);
//
//     /// <summary>
//     ///     Delete a container if it does not already exist.
//     /// </summary>
//     Task RemoveContainerAsync(CancellationToken cancellationToken = default);
//
//     Task DeleteDirectoryAsync(string directory, CancellationToken cancellationToken = default);
// }
//
// public interface IStorageOptions<TOptions>
// {
//     Task SetStorageOptions(TOptions options, CancellationToken cancellationToken = default);
//     Task SetStorageOptions(Action<TOptions> options, CancellationToken cancellationToken = default);
// }
//
// public interface IMetaDataReader<TMetaData>
// {
//     public Task<TMetaData> GetMetaDataAsync(Action<IFileChooser> file, CancellationToken token = default);
//
//     IAsyncEnumerable<TMetaData> GetBlobMetadataListAsync(string? directory = null, CancellationToken cancellationToken = default);
// }
//
// public interface ILegalHold
// {
//     public Task SetLegalHoldAsync(Action<IFileChooser> file, bool legalHoldStatus, CancellationToken cancellationToken = default);
//
//     public Task HasLegalHold(Action<IFileChooser> file, CancellationToken cancellationToken = default);
// }
//
// public interface IFileUploader<TResult, TOptions>
//     where TOptions : class
// {
//     public Task<TResult> UploadAsync(Action<IFileReader> file, TOptions? options = null,
//         ProgressHandler? progressHandler = null, CancellationToken? token = null);
//
// }
//
//
// public interface IFileDownloader<TResult, TOptions>
//     where TOptions : class
// {
//     public Task<TResult> DownloadAsync(Action<IFileChooser> fileChooser, TOptions? options = null,
//         ProgressHandler? progressHandler = null, CancellationToken? token = null);
// }
//
// public interface IFileDeleter<TResult, TOptions>
// where TOptions : class
// {
//     public Task<TResult> DeleteAsync(Action<IFileChooser> file, TOptions? options = null, CancellationToken? token = null);
// }
//
//
// public interface IFileChooser
// {
//     public IFileChooser FromUrl(string url);
//
//     public void FromDirectory(string directory, string fileName);
// }
//
// public class DownloadOptions
// {
//
// }
//
// public class UploadOptions
// {
//
// }
//
// public class UploadResult
// {
//
// }
//
// public delegate void ProgressHandler(object sender, ProgressArgs args);
//
// public class ProgressArgs
// {
//
// }
//
//
// public interface IFileClient : IFileUploader, IFileDownloader
// {
//
// }
//
// public interface IFileReader
// {
//     public void FromPath(string filePath);
//
//     public void FromFileInfo(FileInfo info);
//
//     public void FromStream(Stream stream);
//
//     public void FromBytes(byte[] bytes);
// }
//
// internal class FileReader : IFileReader
// {
//     public void FromPath(string filePath)
//     {
//         throw new NotImplementedException();
//     }
//
//     public void FromFileInfo(FileInfo info)
//     {
//         throw new NotImplementedException();
//     }
//
//     public void FromStream(Stream stream)
//     {
//         throw new NotImplementedException();
//     }
//
//     public void FromBytes(byte[] bytes)
//     {
//         throw new NotImplementedException();
//     }
// }

