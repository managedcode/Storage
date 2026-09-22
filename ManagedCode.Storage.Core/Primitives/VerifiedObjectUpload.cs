using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;

namespace ManagedCode.Storage.Core.Primitives;

public sealed record VerifiedObjectUploadResult(StorageObjectInfo Info, string Sha256, bool ReusedExisting);

public sealed class StorageUploadLengthException(bool tooLarge)
    : IOException(tooLarge ? "Upload exceeds its declared length." : "Upload is shorter than its declared length.")
{
    public bool TooLarge { get; } = tooLarge;
}

public static class VerifiedObjectUpload
{
    private const int PartBytes = 4 * 1024 * 1024;

    /// <summary>Stages and verifies an immutable upload before atomic commit. Identical retries reuse the existing revision.</summary>
    public static async Task<VerifiedObjectUploadResult> WriteIfAbsentOrSameAsync(
        this IObjectStorage storage,
        string path,
        Stream content,
        long expectedLength,
        StorageWriteOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(storage);
        ArgumentNullException.ThrowIfNull(content);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(expectedLength);
        if (storage is not IMultipartObjectStorage multipart)
        {
            throw new NotSupportedException("Verified immutable upload requires staged multipart storage.");
        }

        options ??= new StorageWriteOptions();
        if (options.IfMatch is not null)
        {
            throw new ArgumentException("An immutable upload cannot use IfMatch.", nameof(options));
        }

        var (partIds, inputHash) = await StageAsync(multipart, path, content, expectedLength,
            cancellationToken).ConfigureAwait(false);
        StorageObjectInfo info;
        var reused = false;
        try
        {
            info = await multipart.CommitPartsAsync(path, partIds,
                options with { IfAbsent = true }, cancellationToken).ConfigureAwait(false);
        }
        catch (StorageOperationException conflict) when (conflict.IsConflict)
        {
            info = await multipart.GetObjectInfoAsync(path, cancellationToken).ConfigureAwait(false);
            if (info.Length != expectedLength ||
                (options.ContentType is not null &&
                 !string.Equals(info.ContentType, options.ContentType, StringComparison.OrdinalIgnoreCase)) ||
                (options.ContentEncoding is not null &&
                 !string.Equals(info.ContentEncoding, options.ContentEncoding, StringComparison.OrdinalIgnoreCase)) ||
                (options.Metadata is not null && !MetadataMatches(options.Metadata, info.Metadata)) ||
                !await MatchesExistingAsync(multipart, path, info.ETag, expectedLength,
                    inputHash, cancellationToken).ConfigureAwait(false))
            {
                throw;
            }

            reused = true;
        }

        if (info.Length != expectedLength)
        {
            throw new StorageUploadLengthException(info.Length > expectedLength);
        }

        return new VerifiedObjectUploadResult(info, Convert.ToHexString(inputHash).ToLowerInvariant(), reused);
    }

    private static async Task<(IReadOnlyList<string> PartIds, byte[] Hash)> StageAsync(
        IMultipartObjectStorage storage, string path, Stream content, long expectedLength,
        CancellationToken cancellationToken)
    {
        using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        var ids = new List<string>();
        var transferId = RandomNumberGenerator.GetBytes(8);
        var buffer = new byte[PartBytes];
        long total = 0;
        while (total < expectedLength)
        {
            var wanted = (int)Math.Min(buffer.Length, expectedLength - total);
            var partLength = await ReadPartAsync(content, buffer, wanted, cancellationToken).ConfigureAwait(false);
            if (partLength == 0)
            {
                throw new StorageUploadLengthException(false);
            }

            hash.AppendData(buffer.AsSpan(0, partLength));
            var partId = PartId(transferId, ids.Count);
            using (var part = new MemoryStream(buffer, 0, partLength, writable: false))
            {
                await storage.StagePartAsync(path, partId, part, cancellationToken).ConfigureAwait(false);
            }

            ids.Add(partId);
            total += partLength;
        }

        if (await content.ReadAsync(buffer.AsMemory(0, 1), cancellationToken).ConfigureAwait(false) != 0)
        {
            throw new StorageUploadLengthException(true);
        }

        return (ids, hash.GetHashAndReset());
    }

    private static async Task<int> ReadPartAsync(Stream content, byte[] buffer, int wanted,
        CancellationToken cancellationToken)
    {
        var length = 0;
        while (length < wanted)
        {
            var read = await content.ReadAsync(buffer.AsMemory(length, wanted - length),
                cancellationToken).ConfigureAwait(false);
            if (read == 0)
            {
                break;
            }

            length += read;
        }

        return length;
    }

    private static string PartId(byte[] transferId, int index)
    {
        var value = new byte[16];
        transferId.CopyTo(value, 0);
        BitConverter.TryWriteBytes(value.AsSpan(8), index);
        return Convert.ToBase64String(value);
    }

    private static bool MetadataMatches(IReadOnlyDictionary<string, string> expected,
        IReadOnlyDictionary<string, string> actual)
    {
        if (expected.Count != actual.Count)
        {
            return false;
        }

        foreach (var item in expected)
        {
            var found = false;
            foreach (var stored in actual)
            {
                if (!string.Equals(item.Key, stored.Key, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (!string.Equals(item.Value, stored.Value, StringComparison.Ordinal))
                {
                    return false;
                }

                found = true;
                break;
            }

            if (!found)
            {
                return false;
            }
        }

        return true;
    }

    private static async Task<bool> MatchesExistingAsync(IObjectStorage storage, string path,
        string etag, long expectedLength, byte[] inputHash, CancellationToken cancellationToken)
    {
        await using var existing = await storage.OpenObjectReadAsync(path,
            new StorageReadOptions { IfMatch = etag }, cancellationToken).ConfigureAwait(false);
        var storedHash = await HashExactAsync(existing, expectedLength, cancellationToken).ConfigureAwait(false);
        return CryptographicOperations.FixedTimeEquals(inputHash, storedHash);
    }

    private static async Task<byte[]> HashExactAsync(Stream content, long expectedLength,
        CancellationToken cancellationToken)
    {
        using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        var buffer = new byte[81920];
        long total = 0;
        while (true)
        {
            var read = await content.ReadAsync(buffer, cancellationToken).ConfigureAwait(false);
            if (read == 0)
            {
                break;
            }

            if (read > expectedLength - total)
            {
                throw new StorageUploadLengthException(true);
            }

            hash.AppendData(buffer.AsSpan(0, read));
            total += read;
        }

        if (total != expectedLength)
        {
            throw new StorageUploadLengthException(false);
        }

        return hash.GetHashAndReset();
    }
}
