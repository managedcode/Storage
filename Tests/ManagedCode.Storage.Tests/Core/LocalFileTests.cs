using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using ManagedCode.Storage.Core.Models;
using Shouldly;
using Xunit;

namespace ManagedCode.Storage.Tests.Core;

public class LocalFileTests
{
    [Fact]
    public async Task OpenReadStream_DisposeOwnerByDefault_DeletesBackingFile()
    {
        var localFile = LocalFile.FromTempFile();
        var filePath = localFile.FilePath;

        var payload = Encoding.UTF8.GetBytes("ping");
        localFile.WriteAllBytes(payload);
        File.Exists(filePath).ShouldBeTrue();

        await using (var stream = localFile.OpenReadStream())
        {
            var buffer = new byte[payload.Length];
            var read = await stream.ReadAsync(buffer, 0, buffer.Length);
            read.ShouldBe(buffer.Length);
            buffer.ShouldBe(payload);
        }

        File.Exists(filePath).ShouldBeFalse();
    }

    [Fact]
    public async Task OpenReadStream_DisposeOwnerFalse_PreservesBackingFile()
    {
        await using var localFile = LocalFile.FromTempFile();
        var filePath = localFile.FilePath;

        localFile.WriteAllText("pong");
        File.Exists(filePath).ShouldBeTrue();

        await using (var stream = localFile.OpenReadStream(disposeOwner: false))
        {
            var reader = new StreamReader(stream, leaveOpen: false);
            var text = await reader.ReadToEndAsync();
            text.ShouldBe("pong");
        }

        File.Exists(filePath).ShouldBeTrue();
    }

    [Fact]
    public async Task LocalFile_Finalizer_RemovesFile_WhenNotDisposed()
    {
        string filePath;
        var weakReference = CreateUntrackedLocalFile(out filePath);

        File.Exists(filePath).ShouldBeTrue();

        var deleted = await WaitForFileDeletionAsync(filePath, weakReference);

        deleted.ShouldBeTrue($"File '{filePath}' should be deleted once LocalFile is finalized.");
        File.Exists(filePath).ShouldBeFalse();
    }

    private static WeakReference CreateUntrackedLocalFile(out string filePath)
    {
        var file = LocalFile.FromTempFile();
        filePath = file.FilePath;

        file.WriteAllText("ghost");

        using (var stream = file.OpenReadStream(disposeOwner: false))
        {
            var buffer = new byte[5];
            _ = stream.Read(buffer, 0, buffer.Length);
        }

        var weakReference = new WeakReference(file);
        file = null!;
        return weakReference;
    }

    private static async Task<bool> WaitForFileDeletionAsync(string filePath, WeakReference weakReference)
    {
        const int maxAttempts = 20;
        for (var attempt = 0; attempt < maxAttempts; attempt++)
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            if (!weakReference.IsAlive && !File.Exists(filePath))
            {
                return true;
            }

            await Task.Delay(100);
        }

        return !weakReference.IsAlive && !File.Exists(filePath);
    }
}
