using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using ManagedCode.Communication;
using ManagedCode.Storage.Core.Models;

namespace ManagedCode.Storage.Core.Extensions;

/// <summary>Transfers files between configured storage instances.</summary>
public static class StorageTransferExtensions
{
    /// <summary>Copies the source file to a vacant destination path and returns the destination metadata.</summary>
    /// <remarks>Callers must coordinate other writes and deletes to either path during the transfer.
    /// The destination provider controls which content types and custom metadata it can persist.</remarks>
    public static Task<Result<BlobMetadata>> CopyToAsync(this IStorage source, string sourcePath,
        IStorage destination, string destinationPath, CancellationToken cancellationToken = default) =>
        TransferAsync(source, sourcePath, destination, destinationPath, move: false, cancellationToken);

    /// <summary>Copies the source file and removes it after the destination write succeeds.</summary>
    /// <remarks>Callers must coordinate other writes and deletes to either path during the transfer.
    /// If source deletion fails after the copy, the operation reports failure and keeps the destination file.</remarks>
    public static Task<Result<BlobMetadata>> MoveToAsync(this IStorage source, string sourcePath,
        IStorage destination, string destinationPath, CancellationToken cancellationToken = default) =>
        TransferAsync(source, sourcePath, destination, destinationPath, move: true, cancellationToken);

    private static async Task<Result<BlobMetadata>> TransferAsync(IStorage source, string sourcePath,
        IStorage destination, string destinationPath, bool move, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(destination);
        ArgumentException.ThrowIfNullOrWhiteSpace(sourcePath);
        ArgumentException.ThrowIfNullOrWhiteSpace(destinationPath);
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            var copied = await CopyFileAsync(source, sourcePath, destination, destinationPath, cancellationToken).ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();
            if (!move || copied.IsFailed)
                return copied;

            var deleted = await source.DeleteAsync(sourcePath, cancellationToken).ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();
            if (deleted.IsFailed)
                return Result<BlobMetadata>.Fail(deleted.Problem);

            return deleted.Value
                ? copied
                : Result<BlobMetadata>.Fail("The destination was written, but the source deletion was not confirmed.");
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Result<BlobMetadata>.Fail(exception);
        }
    }

    private static async Task<Result<BlobMetadata>> CopyFileAsync(IStorage source, string sourcePath,
        IStorage destination, string destinationPath, CancellationToken cancellationToken)
    {
        var metadata = await source.GetBlobMetadataAsync(sourcePath, cancellationToken).ConfigureAwait(false);
        cancellationToken.ThrowIfCancellationRequested();
        if (metadata.IsFailed)
            return metadata;

        var exists = await destination.ExistsAsync(destinationPath, cancellationToken).ConfigureAwait(false);
        cancellationToken.ThrowIfCancellationRequested();
        if (exists.IsFailed)
            return Result<BlobMetadata>.Fail(exists.Problem);
        if (exists.Value)
            return Result<BlobMetadata>.Fail("Destination already exists", destinationPath, HttpStatusCode.Conflict);

        var opened = await source.GetStreamAsync(sourcePath, cancellationToken).ConfigureAwait(false);
        if (opened.IsFailed)
            return Result<BlobMetadata>.Fail(opened.Problem);

        var content = opened.Value;
        await using (content.ConfigureAwait(false))
        {
            cancellationToken.ThrowIfCancellationRequested();
            var info = metadata.Value;
            var customMetadata = info.Metadata is null ? null : new Dictionary<string, string>(info.Metadata, info.Metadata.Comparer);
            var options = new UploadOptions(destinationPath, mimeType: info.MimeType, metadata: customMetadata);

            if (content.CanSeek)
                return await UploadAsync(destination, content, options, info.Length, cancellationToken).ConfigureAwait(false);

            // Destination SDKs may need to determine the length or rewind for retries.
            var staged = await LocalFile.FromStreamAsync(content, cancellationToken).ConfigureAwait(false);
            await using (staged.ConfigureAwait(false))
            {
                return await UploadAsync(destination, staged.FileStream, options, info.Length, cancellationToken).ConfigureAwait(false);
            }
        }
    }

    private static async Task<Result<BlobMetadata>> UploadAsync(IStorage destination, Stream content,
        UploadOptions options, ulong expectedLength, CancellationToken cancellationToken)
    {
        if ((ulong)content.Length != expectedLength)
            return Result<BlobMetadata>.Fail("Source length differs from its storage metadata.");

        var uploaded = await destination.UploadAsync(content, options, cancellationToken).ConfigureAwait(false);
        if (uploaded.IsSuccess && uploaded.Value.Length != expectedLength)
            return Result<BlobMetadata>.Fail("Destination length differs from the source length.");

        return uploaded;
    }
}
