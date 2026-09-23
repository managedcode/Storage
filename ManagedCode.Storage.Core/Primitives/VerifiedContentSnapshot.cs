using System;
using System.Buffers;
using System.IO;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;

namespace ManagedCode.Storage.Core.Primitives;

/// <summary>Creates a bounded, file-backed snapshot only when length and SHA-256 match.</summary>
public static class VerifiedContentSnapshot
{
    /// <remarks>The caller owns source; the caller must dispose a successful snapshot.</remarks>
    public static async Task<Stream?> CreateAsync(
        Stream source, long expectedLength, string expectedSha256, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(expectedSha256);
        if (expectedLength < 0)
        {
            return null;
        }

        var options = new FileStreamOptions
        {
            Mode = FileMode.CreateNew,
            Access = FileAccess.ReadWrite,
            Share = FileShare.None,
            Options = FileOptions.Asynchronous | FileOptions.DeleteOnClose
        };
        if (!OperatingSystem.IsWindows())
        {
            options.UnixCreateMode = UnixFileMode.UserRead | UnixFileMode.UserWrite;
        }

        var snapshot = new FileStream(Path.Combine(Path.GetTempPath(), Path.GetRandomFileName()), options);
        try
        {
            using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
            using var buffer = MemoryPool<byte>.Shared.Rent();
            long length = 0;
            while (true)
            {
                var read = await source.ReadAsync(buffer.Memory, cancellationToken).ConfigureAwait(false);
                if (read == 0)
                {
                    break;
                }

                length = checked(length + read);
                if (length > expectedLength)
                {
                    await snapshot.DisposeAsync().ConfigureAwait(false);
                    return null;
                }

                hash.AppendData(buffer.Memory.Span[..read]);
                await snapshot.WriteAsync(buffer.Memory[..read], cancellationToken).ConfigureAwait(false);
            }

            if (length != expectedLength ||
                !string.Equals(Convert.ToHexStringLower(hash.GetHashAndReset()), expectedSha256, StringComparison.Ordinal))
            {
                await snapshot.DisposeAsync().ConfigureAwait(false);
                return null;
            }

            snapshot.Position = 0;
            return snapshot;
        }
        catch
        {
            await snapshot.DisposeAsync().ConfigureAwait(false);
            throw;
        }
    }
}
