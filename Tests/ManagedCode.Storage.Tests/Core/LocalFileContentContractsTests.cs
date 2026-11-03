using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ManagedCode.Storage.Core;
using ManagedCode.Storage.Core.Models;
using Shouldly;
using Xunit;

namespace ManagedCode.Storage.Tests.Core;

public sealed class LocalFileContentContractsTests
{
    [Fact]
    public async Task ExistingExtensionlessFile_PreservesItsPathAndBytes()
    {
        var root = Path.Combine(Path.GetTempPath(), "extensionless-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            var path = Path.Combine(root, "plain");
            var bytes = System.Text.Encoding.UTF8.GetBytes("extensionless content");
            await File.WriteAllBytesAsync(path, bytes, TestContext.Current.CancellationToken);
            await using var local = new LocalFile(path, keepAlive: true);
            local.FilePath.ShouldBe(path);
            (await local.ReadAllBytesAsync(TestContext.Current.CancellationToken)).ShouldBe(bytes);
        }
        finally { Directory.Delete(root, recursive: true); }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task NamedStreamFactory_ReplacesExistingContentExactly(bool useDirectory)
    {
        var token = TestContext.Current.CancellationToken;
        var name = $"local-contract-{Guid.NewGuid():N}.txt";
        var path = Path.Combine(Path.GetTempPath(), useDirectory ? $"{name}.tmp" : name);
        await File.WriteAllTextAsync(path, "longer existing content", token);
        try
        {
            using var source = new Utf8StringStream("short");
            await using var file = useDirectory
                ? await LocalFile.FromStreamAsync(source, Path.GetTempPath(), name, token)
                : await LocalFile.FromStreamAsync(source, name, token);
            (await file.ReadAllTextAsync(token)).ShouldBe("short");
            source.CanRead.ShouldBeTrue();
        }
        finally { File.Delete(path); }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task NamedStreamFactory_ReleasesDestinationOnCopyFailure(bool existing)
    {
        var token = TestContext.Current.CancellationToken;
        var name = $"local-contract-{Guid.NewGuid():N}.txt";
        var path = Path.Combine(Path.GetTempPath(), name);
        if (existing)
            await File.WriteAllTextAsync(path, "existing content", token);

        try
        {
            var source = new MemoryStream();
            await source.DisposeAsync();
            await Should.ThrowAsync<ObjectDisposedException>(() => LocalFile.FromStreamAsync(source, name, token));
            File.Exists(path).ShouldBe(existing);
            if (existing)
            {
                await using var exclusive = File.Open(path, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
                exclusive.CanWrite.ShouldBeTrue();
            }
        }
        finally { File.Delete(path); }
    }

    [Fact]
    public async Task NamedStreamFactory_PreCancelledCopyPreservesExistingBytes()
    {
        var token = TestContext.Current.CancellationToken;
        var name = $"local-contract-{Guid.NewGuid():N}.txt";
        var path = Path.Combine(Path.GetTempPath(), name);
        await File.WriteAllTextAsync(path, "existing content", token);
        try
        {
            using var source = new Utf8StringStream("replacement");
            await Should.ThrowAsync<OperationCanceledException>(
                () => LocalFile.FromStreamAsync(source, name, new CancellationToken(canceled: true)));
            (await File.ReadAllTextAsync(path, token)).ShouldBe("existing content");
        }
        finally { File.Delete(path); }
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public async Task StreamFactoriesAndContentApis_PersistExactData(int factory)
    {
        var token = TestContext.Current.CancellationToken;
        await using var source = new Utf8StringStream("original content");
        var name = "local-contract-" + Guid.NewGuid().ToString("N") + ".txt";
        await using var file = factory switch
        {
            0 => await LocalFile.FromStreamAsync(source, token),
            1 => await LocalFile.FromStreamAsync(source, name, token),
            _ => await LocalFile.FromStreamAsync(source, Path.GetTempPath(), name, token)
        };
        file.ReadAllText().ShouldBe("original content");
        using (var replacement = new Utf8StringStream("short"))
            (await file.CopyFromStreamAsync(replacement, token)).ShouldBeSameAs(file);
        (await file.ReadAllTextAsync(token)).ShouldBe("short");
        file.WriteAllText("text");
        (await file.ReadAllTextAsync(token)).ShouldBe("text");
        await file.WriteAllTextAsync("async text", token);
        file.ReadAllText().ShouldBe("async text");
        var lines = new[] { "first", "Привіт" };
        file.WriteAllLines(lines);
        file.ReadAllLines().ShouldBe(lines);
        (await file.ReadAllLinesAsync(token)).ShouldBe(lines);
        file.ReadLines().ToArray().ShouldBe(lines);
        await file.WriteAllLinesAsync(new[] { "replacement" }, token);
        file.ReadAllLines().ShouldBe(new[] { "replacement" });
        file.WriteAllBytes(new byte[] { 1, 2, 3 });
        file.ReadAllBytes().ShouldBe(new byte[] { 1, 2, 3 });
        await file.WriteAllBytesAsync(new byte[] { 4, 5 }, token);
        (await file.ReadAllBytesAsync(token)).ShouldBe(new byte[] { 4, 5 });
        file.Close();
        file.FileInfo.Length.ShouldBe(2);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task ReadStream_ExposesRealCapabilitiesAndOwnership(bool disposeOwner)
    {
        var token = TestContext.Current.CancellationToken;
        await using var file = LocalFile.FromTempFile();
        await file.WriteAllBytesAsync(new byte[] { 10, 20, 30, 40 }, token);
        var stream = file.OpenReadStream(disposeOwner);
        stream.CanRead.ShouldBeTrue();
        stream.CanSeek.ShouldBeTrue();
        stream.CanWrite.ShouldBeFalse();
        stream.Length.ShouldBe(4);
        stream.ReadByte().ShouldBe(10);
        stream.Position = 0;
        var buffer = new byte[4];
        stream.Read(buffer, 0, 1).ShouldBe(1);
        stream.Read(buffer.AsSpan(1, 1)).ShouldBe(1);
        (await stream.ReadAsync(buffer, 2, 1, token)).ShouldBe(1);
        (await stream.ReadAsync(buffer.AsMemory(3, 1), token)).ShouldBe(1);
        buffer.ShouldBe(new byte[] { 10, 20, 30, 40 });
        stream.Seek(-1, SeekOrigin.End).ShouldBe(3);
        stream.Flush();
        await stream.FlushAsync(token);
        Should.Throw<NotSupportedException>(() => stream.Write(buffer, 0, 1));
        Should.Throw<NotSupportedException>(() => stream.Write(buffer.AsSpan()));
        await Should.ThrowAsync<NotSupportedException>(() => stream.WriteAsync(buffer, 0, 1, token));
        await Should.ThrowAsync<NotSupportedException>(async () => await stream.WriteAsync(buffer.AsMemory(), token));
        Should.Throw<NotSupportedException>(() => stream.SetLength(1));
        await stream.DisposeAsync();
        await stream.DisposeAsync();
        File.Exists(file.FilePath).ShouldBe(!disposeOwner);
        stream.CanRead.ShouldBeFalse();
        if (!disposeOwner)
            file.ReadAllBytes().ShouldBe(buffer);
    }

    [Fact]
    public void MissingBackingFile_IsReportedAsFileNotFound()
    {
        using var file = LocalFile.FromRandomNameWithExtension("file.dat");
        Path.GetExtension(file.FilePath).ShouldBe(".dat");
        File.Delete(file.FilePath);
        Should.Throw<FileNotFoundException>(() => _ = file.FileStream);
        Should.Throw<FileNotFoundException>(() => file.OpenReadStream());
    }
}
