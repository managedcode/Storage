using System;
using System.Buffers;
using System.IO;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;

namespace ManagedCode.Storage.Core.Primitives;

/// <summary>The uploaded content does not match the SHA-256 its caller declared; nothing was committed.</summary>
public sealed class StorageUploadDigestException()
    : IOException("Upload content does not match its declared SHA-256.");

public static partial class VerifiedObjectUpload
{
    private const int Sha256ByteLength = 32;
    private const int Sha256HexLength = Sha256ByteLength * 2;

    /// <summary>
    /// Stages and verifies an immutable upload against its declared length and hexadecimal SHA-256. A digest mismatch
    /// throws <see cref="StorageUploadDigestException"/> before commit; identical retries reuse the existing revision.
    /// </summary>
    public static async Task<VerifiedObjectUploadResult> WriteIfAbsentOrSameAsync(
        this IObjectStorage storage,
        string path,
        Stream content,
        long expectedLength,
        string expectedSha256,
        StorageWriteOptions? options = null,
        CancellationToken cancellationToken = default) =>
        await WriteVerifiedAsync(storage, path, content, expectedLength, ParseSha256(expectedSha256), options,
            cancellationToken).ConfigureAwait(false);

    /// <summary>
    /// Atomically writes a small immutable object whose content must match the hexadecimal SHA-256. A digest mismatch
    /// throws <see cref="StorageUploadDigestException"/> before anything is written.
    /// </summary>
    public static async Task<VerifiedObjectUploadResult> WriteBytesIfAbsentOrSameAsync(
        this IObjectStorage storage,
        string path,
        ReadOnlyMemory<byte> content,
        string expectedSha256,
        StorageWriteOptions? options = null,
        CancellationToken cancellationToken = default) =>
        await WriteBytesVerifiedAsync(storage, path, content, ParseSha256(expectedSha256), options,
            cancellationToken).ConfigureAwait(false);

    private static byte[] ParseSha256(string expectedSha256)
    {
        ArgumentNullException.ThrowIfNull(expectedSha256);
        var digest = new byte[Sha256ByteLength];
        if (expectedSha256.Length != Sha256HexLength ||
            Convert.FromHexString(expectedSha256, digest, out _, out _) != OperationStatus.Done)
        {
            throw new ArgumentException("The expected SHA-256 must be 64 hexadecimal characters.",
                nameof(expectedSha256));
        }

        return digest;
    }

    private static void ThrowIfDigestDiffers(byte[] actual, byte[]? expected)
    {
        if (expected is not null && !CryptographicOperations.FixedTimeEquals(actual, expected))
        {
            throw new StorageUploadDigestException();
        }
    }
}
