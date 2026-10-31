using System;
using System.IO;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;

namespace ManagedCode.Storage.Core.Primitives;

public static partial class VerifiedObjectUpload
{
    /// <summary>Atomically writes a small immutable object and accepts only an exact existing retry.</summary>
    public static Task<VerifiedObjectUploadResult> WriteBytesIfAbsentOrSameAsync(
        this IObjectStorage storage,
        string path,
        ReadOnlyMemory<byte> content,
        StorageWriteOptions? options = null,
        CancellationToken cancellationToken = default) =>
        WriteBytesVerifiedAsync(storage, path, content, null, options, cancellationToken);

    private static async Task<VerifiedObjectUploadResult> WriteBytesVerifiedAsync(
        IObjectStorage storage,
        string path,
        ReadOnlyMemory<byte> content,
        byte[]? expectedSha256,
        StorageWriteOptions? options,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(storage);
        options ??= new StorageWriteOptions();
        if (options.IfMatch is not null)
        {
            throw new ArgumentException("An immutable write cannot use IfMatch.", nameof(options));
        }

        var contentHash = SHA256.HashData(content.Span);
        ThrowIfDigestDiffers(contentHash, expectedSha256);
        StorageObjectInfo info;
        var reused = false;
        try
        {
            info = await storage.WriteObjectAsync(path, content, options with { IfAbsent = true },
                cancellationToken).ConfigureAwait(false);
        }
        catch (IOException) when (!cancellationToken.IsCancellationRequested)
        {
            info = await storage.GetObjectInfoAsync(path, cancellationToken).ConfigureAwait(false);
            if (info.Length != content.Length ||
                (options.ContentType is not null &&
                 !string.Equals(info.ContentType, options.ContentType, StringComparison.OrdinalIgnoreCase)) ||
                (options.ContentEncoding is not null &&
                 !string.Equals(info.ContentEncoding, options.ContentEncoding, StringComparison.OrdinalIgnoreCase)) ||
                (options.Metadata is not null && !MetadataMatches(options.Metadata, info.Metadata)) ||
                !await MatchesExistingAsync(storage, path, info.ETag, content.Length,
                    contentHash, cancellationToken).ConfigureAwait(false))
            {
                throw;
            }

            reused = true;
        }

        if (info.Length != content.Length)
        {
            throw new StorageUploadLengthException(info.Length > content.Length);
        }

        return new VerifiedObjectUploadResult(info, Convert.ToHexStringLower(contentHash), reused);
    }
}
