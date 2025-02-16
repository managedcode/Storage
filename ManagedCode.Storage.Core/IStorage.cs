using System;
using System.Threading;
using System.Threading.Tasks;
using ManagedCode.Communication;

namespace ManagedCode.Storage.Core
{
    /// <summary>
    /// Represents a generic storage interface with a specific client and options.
    /// </summary>
    /// <typeparam name="T">The type of the storage client.</typeparam>
    /// <typeparam name="TOptions">The type of the storage options.</typeparam>
    public interface IStorage<out T, TOptions> : IStorage where TOptions : IStorageOptions
    {
        /// <summary>
        /// Gets the storage client.
        /// </summary>
        T StorageClient { get; }

        /// <summary>
        /// Sets the storage options asynchronously.
        /// </summary>
        /// <param name="options">The options to set.</param>
        /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the result of the operation.</returns>
        Task<Result> SetStorageOptions(TOptions options, CancellationToken cancellationToken = default);

        /// <summary>
        /// Sets the storage options asynchronously with the specified action to configure the options.
        /// </summary>
        /// <param name="options">An action to configure the options.</param>
        /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the result of the operation.</returns>
        Task<Result> SetStorageOptions(Action<TOptions> options, CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Represents a storage interface that includes uploader, downloader, streamer, and storage operations.
    /// </summary>
    public interface IStorage : IUploader, IDownloader, IStreamer, IStorageOperations, IDisposable
    {
        /// <summary>
        /// Creates a container asynchronously if it does not already exist.
        /// </summary>
        /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the result of the operation.</returns>
        Task<Result> CreateContainerAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Removes a container asynchronously if it exists.
        /// </summary>
        /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the result of the operation.</returns>
        Task<Result> RemoveContainerAsync(CancellationToken cancellationToken = default);
    }
}