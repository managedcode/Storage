using System;
using System.IO;
using System.Buffers;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using ManagedCode.Storage.Core.Helpers;
using ManagedCode.Storage.Core.Models;
using Xunit;
using Xunit.Abstractions;

namespace ManagedCode.Storage.Tests.Common;

public static class LargeFileTestHelper
{
    /// <summary>
    /// Base unit (in bytes) used when synthesising large-file test payloads. Keeps runtime manageable while
    /// exercising multi-chunk flows across transports. Equivalent to 64 MB.
    /// </summary>
    public const long LargeFileUnitBytes = 16L * 1024L * 1024L;

    /// <summary>
    /// Resolves the byte-length used for a given "gigabyte" unit in large file tests. The multiplier keeps
    /// execution time practical for local and CI runs while still stressing streaming code paths.
    /// </summary>
    /// <param name="gigabyteUnits">Logical gigabyte input (1, 3, 5, ...).</param>
    /// <returns>Total bytes to generate for the test case.</returns>
    public static long ResolveSizeBytes(int gigabyteUnits)
    {
        if (gigabyteUnits <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(gigabyteUnits));
        }

        return gigabyteUnits * LargeFileUnitBytes;
    }

    public static async Task<LocalFile> CreateRandomFileAsync(long sizeBytes, string extension = ".bin", int bufferSize = 4 * 1024 * 1024, CancellationToken cancellationToken = default)
    {
        if (sizeBytes <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(sizeBytes));
        }

        if (bufferSize <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(bufferSize));
        }

        var directory = Path.Combine(Environment.CurrentDirectory, "large-file-tests");
        Directory.CreateDirectory(directory);
        var filePath = Path.Combine(directory, Guid.NewGuid().ToString("N") + extension);
        var file = new LocalFile(filePath);
        await using var fileStream = file.FileStream;
        var buffer = ArrayPool<byte>.Shared.Rent(bufferSize);

        try
        {
            long remaining = sizeBytes;
            using var rng = RandomNumberGenerator.Create();

            while (remaining > 0)
            {
                var toWrite = (int)Math.Min(bufferSize, remaining);
                rng.GetBytes(buffer, 0, toWrite);
                await fileStream.WriteAsync(buffer.AsMemory(0, toWrite), cancellationToken).ConfigureAwait(false);
                remaining -= toWrite;
            }

            await fileStream.FlushAsync(cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
            await fileStream.DisposeAsync().ConfigureAwait(false);
        }

        return file;
    }

    public static uint CalculateFileCrc(string path)
    {
        using var stream = File.OpenRead(path);
        return Crc32Helper.CalculateStreamCrc(stream);
    }

    public static uint CalculateStreamCrc(Stream stream)
    {
        return Crc32Helper.CalculateStreamCrc(stream);
    }

    public static void LogFileInfo(LocalFile file, ITestOutputHelper output)
    {
        output.WriteLine($"Generated file: {file.FilePath} ({file.FileInfo.Length} bytes)");
    }
}
