using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ManagedCode.Communication;
using ManagedCode.Storage.Core.Models;

namespace ManagedCode.Storage.Core;

public interface IStorageOperations
{
    /// <summary>
    /// Asynchronously deletes a file.
    /// </summary>
    /// <param name="fileName">The name of the file to delete.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains a Result object with a boolean indicating the success of the operation.</returns>
    Task<Result<bool>> DeleteAsync(string fileName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Asynchronously deletes a file with the provided delete options.
    /// </summary>
    /// <param name="options">The options to use when deleting the file.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains a Result object with a boolean indicating the success of the operation.</returns>
    Task<Result<bool>> DeleteAsync(DeleteOptions options, CancellationToken cancellationToken = default);

    /// <summary>
    /// Asynchronously deletes a file with the provided delete options.
    /// </summary>
    /// <param name="action">An action that configures the delete options.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains a Result object with a boolean indicating the success of the operation.</returns>
    Task<Result<bool>> DeleteAsync(Action<DeleteOptions> action, CancellationToken cancellationToken = default);

    /// <summary>
    /// Asynchronously checks if a file exists.
    /// </summary>
    /// <param name="fileName">The name of the file to check.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains a Result object with a boolean indicating the existence of the file.</returns>
    Task<Result<bool>> ExistsAsync(string fileName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Asynchronously checks if a file exists with the provided exist options.
    /// </summary>
    /// <param name="options">The options to use when checking the file existence.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains a Result object with a boolean indicating the existence of the file.</returns>
    Task<Result<bool>> ExistsAsync(ExistOptions options, CancellationToken cancellationToken = default);


    /// <summary>
    /// Asynchronously checks if a file exists with the provided exist options.
    /// </summary>
    /// <param name="action">An action that configures the exist options.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains a Result object with a boolean indicating the existence of the file.</returns>
    Task<Result<bool>> ExistsAsync(Action<ExistOptions> action, CancellationToken cancellationToken = default);

    /// <summary>
    /// Asynchronously retrieves the metadata of a file.
    /// </summary>
    /// <param name="fileName">The name of the file to retrieve metadata from.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains a Result object with the BlobMetadata of the file.</returns>
    Task<Result<BlobMetadata>> GetBlobMetadataAsync(string fileName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Asynchronously retrieves the metadata of a file with the provided metadata options.
    /// </summary>
    /// <param name="options">The options to use when retrieving the file metadata.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains a Result object with the BlobMetadata of the file.</returns>
    Task<Result<BlobMetadata>> GetBlobMetadataAsync(MetadataOptions options, CancellationToken cancellationToken = default);

    /// <summary>
    /// Asynchronously retrieves the metadata of a file with the provided metadata options.
    /// </summary>
    /// <param name="action">An action that configures the metadata options.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains a Result object with the BlobMetadata of the file.</returns>
    Task<Result<BlobMetadata>> GetBlobMetadataAsync(Action<MetadataOptions> action, CancellationToken cancellationToken = default);

    /// <summary>
    /// Asynchronously retrieves a list of BlobMetadata for all files in the specified directory.
    /// </summary>
    /// <param name="directory">The directory to retrieve the BlobMetadata from. If null, retrieves from all directories.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>An IAsyncEnumerable that can be used to iterate over the BlobMetadata objects.</returns>
    IAsyncEnumerable<BlobMetadata> GetBlobMetadataListAsync(string? directory = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Asynchronously deletes a directory.
    /// </summary>
    /// <param name="directory">The name of the directory to delete.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains a Result object indicating the success of the operation.</returns>
    Task<Result> DeleteDirectoryAsync(string directory, CancellationToken cancellationToken = default);


    /// <summary>
    /// Asynchronously sets the legal hold status for a file.
    /// </summary>
    /// <param name="hasLegalHold">The legal hold status to set.</param>
    /// <param name="fileName">The name of the file to set the legal hold status for.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains a Result object indicating the success of the operation.</returns>
    Task<Result> SetLegalHoldAsync(bool hasLegalHold, string fileName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Asynchronously sets the legal hold status for a file with the provided legal hold options.
    /// </summary>
    /// <param name="hasLegalHold">The legal hold status to set.</param>
    /// <param name="options">The options to use when setting the legal hold status.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains a Result object indicating the success of the operation.</returns>
    Task<Result> SetLegalHoldAsync(bool hasLegalHold, LegalHoldOptions options, CancellationToken cancellationToken = default);

    /// <summary>
    /// Asynchronously sets the legal hold status for a file with the provided legal hold options.
    /// </summary>
    /// <param name="hasLegalHold">The legal hold status to set.</param>
    /// <param name="action">An action that configures the legal hold options.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains a Result object indicating the success of the operation.</returns>
    Task<Result> SetLegalHoldAsync(bool hasLegalHold, Action<LegalHoldOptions> action, CancellationToken cancellationToken = default);

    /// <summary>
    /// Asynchronously checks if a file has a legal hold.
    /// </summary>
    /// <param name="fileName">The name of the file to check.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains a Result object with a boolean indicating the legal hold status of the file.</returns>
    Task<Result<bool>> HasLegalHoldAsync(string fileName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Asynchronously checks if a file has a legal hold with the provided legal hold options.
    /// </summary>
    /// <param name="options">The options to use when checking the legal hold status.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains a Result object with a boolean indicating the legal hold status of the file.</returns>
    Task<Result<bool>> HasLegalHoldAsync(LegalHoldOptions options, CancellationToken cancellationToken = default);

    /// <summary>
    /// Asynchronously checks if a file has a legal hold with the provided legal hold options.
    /// </summary>
    /// <param name="action">An action that configures the legal hold options.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains a Result object with a boolean indicating the legal hold status of the file.</returns>
    Task<Result<bool>> HasLegalHoldAsync(Action<LegalHoldOptions> action, CancellationToken cancellationToken = default);

}