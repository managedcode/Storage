using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ManagedCode.Storage.Core.Models;
using Shouldly;
using Xunit;

namespace ManagedCode.Storage.Tests.Storages.Cartograph;

public sealed class CartographStorageReadTests
{
    [Theory]
    [InlineData("root.txt")]
    [InlineData("reports/large.bin")]
    [InlineData("reports/empty.txt")]
    [InlineData("reports/nested/дані.txt")]
    public async Task GetStreamAsync_WhenCatalogContainsFile_ReadsItsExactBytes(string path)
    {
        using var fixture = new CartographArtifactFixture();
        using var storage = fixture.CreateStorage();
        (await storage.CreateContainerAsync(TestContext.Current.CancellationToken)).IsSuccess.ShouldBeTrue();
        var result = await storage.GetStreamAsync(path, TestContext.Current.CancellationToken);
        result.IsSuccess.ShouldBeTrue();
        await using var stream = result.Value.ShouldNotBeNull();
        stream.CanRead.ShouldBeTrue();
        stream.CanWrite.ShouldBeFalse();
        stream.CanSeek.ShouldBeFalse();
        stream.Length.ShouldBe(fixture.Files[path].LongLength);
        stream.Position.ShouldBe(0);
        using var output = new MemoryStream();
        await stream.CopyToAsync(output, bufferSize: 997, cancellationToken: TestContext.Current.CancellationToken);
        output.ToArray().ShouldBe(fixture.Files[path]);
        stream.Position.ShouldBe(stream.Length);
        stream.ReadByte().ShouldBe(-1);
    }

    [Fact]
    public async Task GetStreamAsync_WhenReadSynchronously_CrossesRecordBoundaries()
    {
        using var fixture = new CartographArtifactFixture();
        using var storage = fixture.CreateStorage();
        using var stream = (await storage.GetStreamAsync("reports/large.bin", TestContext.Current.CancellationToken)).Value.ShouldNotBeNull();
        var actual = new byte[fixture.Files["reports/large.bin"].Length];
        stream.ReadExactly(actual);
        actual.ShouldBe(fixture.Files["reports/large.bin"]);
        stream.Read(Span<byte>.Empty).ShouldBe(0);
        Should.Throw<NotSupportedException>(() => stream.Seek(0, SeekOrigin.Begin));
        Should.Throw<NotSupportedException>(() => stream.WriteByte(1));
        Should.Throw<NotSupportedException>(() => stream.SetLength(1));
        Should.Throw<NotSupportedException>(() => stream.Position = 0);
    }

    [Fact]
    public async Task GetStreamAsync_WhenSeveralReadersRunConcurrently_KeepsSnapshotsIndependent()
    {
        using var fixture = new CartographArtifactFixture();
        using var storage = fixture.CreateStorage();
        var streams = await Task.WhenAll(Enumerable.Range(0, 4).Select(_ => storage.GetStreamAsync("reports/large.bin")));
        storage.Dispose();
        await Task.WhenAll(streams.Select(async result =>
        {
            result.IsSuccess.ShouldBeTrue();
            await using var stream = result.Value.ShouldNotBeNull();
            var actual = new byte[fixture.Files["reports/large.bin"].Length];
            await stream.ReadExactlyAsync(actual);
            actual.ShouldBe(fixture.Files["reports/large.bin"]);
        }));
    }

    [Fact]
    public async Task GetStreamAsync_WhenDisposed_RejectsFurtherReads()
    {
        using var fixture = new CartographArtifactFixture();
        using var storage = fixture.CreateStorage();
        var stream = (await storage.GetStreamAsync("root.txt", TestContext.Current.CancellationToken)).Value.ShouldNotBeNull();
        stream.ReadByte().ShouldBe(fixture.Files["root.txt"][0]);
        await stream.DisposeAsync();
        stream.CanRead.ShouldBeFalse();
        Should.Throw<ObjectDisposedException>(() => stream.ReadByte());
        Should.Throw<ObjectDisposedException>(() => _ = stream.Length);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task DownloadAsync_WhenFileSpansRecords_WritesExactContentAndMetadata(bool explicitPath)
    {
        using var fixture = new CartographArtifactFixture();
        using var storage = fixture.CreateStorage();
        var result = await storage.DownloadAsync(new DownloadOptions
        {
            Directory = "reports",
            FileName = "large.bin",
            LocalPath = explicitPath ? fixture.DestinationPath : null
        }, TestContext.Current.CancellationToken);
        result.IsSuccess.ShouldBeTrue();
        var file = result.Value.ShouldNotBeNull();
        (await File.ReadAllBytesAsync(file.FilePath, TestContext.Current.CancellationToken)).ShouldBe(fixture.Files["reports/large.bin"]);
        file.BlobMetadata.ShouldNotBeNull().FullName.ShouldBe("reports/large.bin");
        var path = file.FilePath;
        await file.DisposeAsync();
        File.Exists(path).ShouldBe(explicitPath);
    }

    [Fact]
    public async Task GetStreamAsync_WhenCancellationRequested_FailsAndCancelsAnOpenRead()
    {
        using var fixture = new CartographArtifactFixture();
        using var storage = fixture.CreateStorage();
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        var result = await storage.GetStreamAsync("root.txt", cancellation.Token);
        result.IsFailed.ShouldBeTrue();
        result.Problem.ShouldNotBeNull().Title.ShouldBe(nameof(OperationCanceledException));
        await using var stream = (await storage.GetStreamAsync("root.txt", TestContext.Current.CancellationToken)).Value.ShouldNotBeNull();
        await Should.ThrowAsync<OperationCanceledException>(async () => await stream.ReadExactlyAsync(new byte[1], cancellation.Token));
        stream.Position.ShouldBe(0);
    }
}
