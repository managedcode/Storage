using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using ManagedCode.Communication;
using ManagedCode.Storage.Core.Models;

namespace ManagedCode.Storage.Core;

public interface IUploader
{

    /// <summary>
    /// Asynchronously uploads the provided stream data to the storage.
    /// </summary>
    /// <param name="stream">The stream data to be uploaded.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the metadata of the uploaded blob.</returns>
    Task<Result<BlobMetadata>> UploadAsync(Stream stream, CancellationToken cancellationToken = default);

    /// <summary>
    /// Asynchronously uploads the provided byte array data to the storage.
    /// </summary>
    /// <param name="data">The byte array data to be uploaded.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the metadata of the uploaded blob.</returns>
    Task<Result<BlobMetadata>> UploadAsync(byte[] data, CancellationToken cancellationToken = default);

    /// <summary>
    /// Asynchronously uploads the provided string content to the storage.
    /// </summary>
    /// <param name="content">The string content to be uploaded.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the metadata of the uploaded blob.</returns>
    Task<Result<BlobMetadata>> UploadAsync(string content, CancellationToken cancellationToken = default);

    /// <summary>
    /// Asynchronously uploads the provided file to the storage.
    /// </summary>
    /// <param name="fileInfo">The file to be uploaded.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the metadata of the uploaded blob.</returns>
    Task<Result<BlobMetadata>> UploadAsync(FileInfo fileInfo, CancellationToken cancellationToken = default);


    /// <summary>
    /// Asynchronously uploads the provided stream data to the storage with the specified upload options.
    /// </summary>
    /// <param name="stream">The stream data to be uploaded.</param>
    /// <param name="options">The options for the upload operation.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the metadata of the uploaded blob.</returns>
    Task<Result<BlobMetadata>> UploadAsync(Stream stream, UploadOptions options, CancellationToken cancellationToken = default);

    /// <summary>
    /// Asynchronously uploads the provided byte array data to the storage with the specified upload options.
    /// </summary>
    /// <param name="data">The byte array data to be uploaded.</param>
    /// <param name="options">The options for the upload operation.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the metadata of the uploaded blob.</returns>
    Task<Result<BlobMetadata>> UploadAsync(byte[] data, UploadOptions options, CancellationToken cancellationToken = default);

    /// <summary>
    /// Asynchronously uploads the provided string content to the storage with the specified upload options.
    /// </summary>
    /// <param name="content">The string content to be uploaded.</param>
    /// <param name="options">The options for the upload operation.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the metadata of the uploaded blob.</returns>
    Task<Result<BlobMetadata>> UploadAsync(string content, UploadOptions options, CancellationToken cancellationToken = default);

    /// <summary>
    /// Asynchronously uploads the provided file to the storage with the specified upload options.
    /// </summary>
    /// <param name="fileInfo">The file to be uploaded.</param>
    /// <param name="options">The options for the upload operation.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the metadata of the uploaded blob.</returns>
    Task<Result<BlobMetadata>> UploadAsync(FileInfo fileInfo, UploadOptions options, CancellationToken cancellationToken = default);

    /// <summary>
    /// Asynchronously uploads the provided stream data to the storage. The upload options are configured by the provided action.
    /// </summary>
    /// <param name="stream">The stream data to be uploaded.</param>
    /// <param name="action">An action that configures the upload options.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the metadata of the uploaded blob.</returns>
    Task<Result<BlobMetadata>> UploadAsync(Stream stream, Action<UploadOptions> action, CancellationToken cancellationToken = default);


    /// <summary>
    /// Asynchronously uploads the provided byte array data to the storage. The upload options are configured by the provided action.
    /// </summary>
    /// <param name="data">The byte array data to be uploaded.</param>
    /// <param name="action">An action that configures the upload options.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the metadata of the uploaded blob.</returns>
    Task<Result<BlobMetadata>> UploadAsync(byte[] data, Action<UploadOptions> action, CancellationToken cancellationToken = default);

    /// <summary>
    /// Asynchronously uploads the provided string content to the storage. The upload options are configured by the provided action.
    /// </summary>
    /// <param name="content">The string content to be uploaded.</param>
    /// <param name="action">An action that configures the upload options.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the metadata of the uploaded blob.</returns>
    Task<Result<BlobMetadata>> UploadAsync(string content, Action<UploadOptions> action, CancellationToken cancellationToken = default);

    /// <summary>
    /// Asynchronously uploads the provided file to the storage. The upload options are configured by the provided action.
    /// </summary>
    /// <param name="fileInfo">The file to be uploaded.</param>
    /// <param name="action">An action that configures the upload options.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the metadata of the uploaded blob.</returns>
    Task<Result<BlobMetadata>> UploadAsync(FileInfo fileInfo, Action<UploadOptions> action, CancellationToken cancellationToken = default);

}