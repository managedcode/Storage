using System;
using System.Threading;
using System.Threading.Tasks;
using ManagedCode.Communication;
using ManagedCode.Storage.Core.Models;

namespace ManagedCode.Storage.Core
{
    /// <summary>
    /// Represents a downloader interface for downloading files.
    /// </summary>
    public interface IDownloader
    {
        /// <summary>
        /// Downloads a file asynchronously.
        /// </summary>
        /// <param name="fileName">The name of the file to download.</param>
        /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the downloaded file.</returns>
        Task<Result<LocalFile>> DownloadAsync(string fileName, CancellationToken cancellationToken = default);

        /// <summary>
        /// Downloads a file asynchronously with the specified download options.
        /// </summary>
        /// <param name="options">The options to use when downloading the file.</param>
        /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the downloaded file.</returns>
        Task<Result<LocalFile>> DownloadAsync(DownloadOptions options, CancellationToken cancellationToken = default);

        /// <summary>
        /// Downloads a file asynchronously with the specified action to configure the download options.
        /// </summary>
        /// <param name="action">An action to configure the download options.</param>
        /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the downloaded file.</returns>
        Task<Result<LocalFile>> DownloadAsync(Action<DownloadOptions> action, CancellationToken cancellationToken = default);
    }
}