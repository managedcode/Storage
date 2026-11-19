using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using ManagedCode.Storage.Core;
using ManagedCode.Storage.Core.Extensions;
using ManagedCode.Storage.Core.Models;
using ManagedCode.Storage.FileSystem;
using ManagedCode.Storage.FileSystem.Options;
using ManagedCode.Storage.Tests.Storages.Cartograph;
using Shouldly;
using Xunit;

namespace ManagedCode.Storage.Tests.Storages;

public sealed class CrossStorageTransferTests : IDisposable
{
    private const string SourcePath = "incoming/дані.txt";
    private const string DestinationPath = "archive/renamed.txt";
    private readonly DirectoryInfo _root = Directory.CreateTempSubdirectory("storage-transfer-");

    [Theory]
    [InlineData(false, "")]
    [InlineData(false, "content to copy")]
    [InlineData(true, "")]
    [InlineData(true, "content to move")]
    public async Task TransfersBetweenRootsAndOnlyMoveRemovesSource(bool move, string content)
    {
        var token = TestContext.Current.CancellationToken;
        using var source = CreateStorage("source");
        using var destination = CreateStorage("destination");
        (await source.UploadAsync(content, new UploadOptions(SourcePath), token)).IsSuccess.ShouldBeTrue();

        var result = move
            ? await source.MoveToAsync(SourcePath, destination, DestinationPath, token)
            : await source.CopyToAsync(SourcePath, destination, DestinationPath, token);

        result.IsSuccess.ShouldBeTrue(result.Problem?.Detail);
        result.Value.FullName.ShouldBe(DestinationPath);
        await AssertContentAsync(destination, DestinationPath, content, token);
        (await source.ExistsAsync(SourcePath, token)).Value.ShouldBe(!move);
        if (!move)
            await AssertContentAsync(source, SourcePath, content, token);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ExistingDestinationKeepsBothFiles(bool alias)
    {
        var token = TestContext.Current.CancellationToken;
        using var source = CreateStorage("source");
        using var destination = CreateStorage(alias ? "source" : "destination");
        var target = alias ? SourcePath : DestinationPath;
        (await source.UploadAsync("original", new UploadOptions(SourcePath), token)).IsSuccess.ShouldBeTrue();
        if (!alias)
            (await destination.UploadAsync("existing", new UploadOptions(target), token)).IsSuccess.ShouldBeTrue();

        var result = await source.MoveToAsync(SourcePath, destination, target, token);

        result.IsFailed.ShouldBeTrue();
        result.Problem.StatusCode.ShouldBe(409);
        await AssertContentAsync(source, SourcePath, "original", token);
        await AssertContentAsync(destination, target, alias ? "original" : "existing", token);
    }

    [Fact]
    public async Task MissingSourceDoesNotCreateDestination()
    {
        var token = TestContext.Current.CancellationToken;
        using var source = CreateStorage("source");
        using var destination = CreateStorage("destination");

        var result = await source.CopyToAsync(SourcePath, destination, DestinationPath, token);

        result.IsFailed.ShouldBeTrue();
        result.Problem.StatusCode.ShouldBe(404);
        (await destination.ExistsAsync(DestinationPath, token)).Value.ShouldBeFalse();
    }

    [Fact]
    public async Task RejectedUploadLeavesSourceReadable()
    {
        var token = TestContext.Current.CancellationToken;
        using var source = CreateStorage("source");
        var folder = _root.CreateSubdirectory("destination");
        folder.CreateSubdirectory(DestinationPath);
        using var destination = CreateStorage("destination");
        (await source.UploadAsync("original", new UploadOptions(SourcePath), token)).IsSuccess.ShouldBeTrue();

        var result = await source.MoveToAsync(SourcePath, destination, DestinationPath, token);

        result.IsFailed.ShouldBeTrue();
        Directory.Exists(Path.Combine(folder.FullName, DestinationPath)).ShouldBeTrue();
        await AssertContentAsync(source, SourcePath, "original", token);
    }

    [Fact]
    public async Task RejectedDeleteLeavesTheCompletedCopyAndSource()
    {
        var token = TestContext.Current.CancellationToken;
        using var artifact = new CartographArtifactFixture();
        using var source = artifact.CreateStorage();
        using var destination = CreateStorage("destination");

        var result = await source.MoveToAsync("root.txt", destination, DestinationPath, token);

        result.IsFailed.ShouldBeTrue();
        const string expected = "Cartograph through Storage\n";
        await AssertContentAsync(source, "root.txt", expected, token);
        await AssertContentAsync(destination, DestinationPath, expected, token);
    }

    [Fact]
    public async Task NonSeekableProviderCopiesMultiRecordFile()
    {
        var token = TestContext.Current.CancellationToken;
        using var artifact = new CartographArtifactFixture();
        using var source = artifact.CreateStorage();
        using var destination = CreateStorage("destination");
        const string original = "reports/large.bin";
        const string target = "archive/large.bin";

        var result = await source.CopyToAsync(original, destination, target, token);

        result.IsSuccess.ShouldBeTrue(result.Problem?.Detail);
        (await File.ReadAllBytesAsync(Path.Combine(_root.FullName, "destination", target), token))
            .ShouldBe(artifact.Files[original]);
    }

    [Fact]
    public async Task FailedSequentialReadLeavesDestinationVacant()
    {
        var token = TestContext.Current.CancellationToken;
        using var artifact = new CartographArtifactFixture();
        artifact.CorruptFirstPayload();
        using var source = artifact.CreateStorage();
        using var destination = CreateStorage("destination");

        var result = await source.CopyToAsync("root.txt", destination, DestinationPath, token);

        result.IsFailed.ShouldBeTrue();
        (await destination.ExistsAsync(DestinationPath, token)).Value.ShouldBeFalse();
    }

    [Fact]
    public async Task CancelledMoveLeavesSourceAndDoesNotCreateDestination()
    {
        var token = TestContext.Current.CancellationToken;
        using var source = CreateStorage("source");
        using var destination = CreateStorage("destination");
        (await source.UploadAsync("original", new UploadOptions(SourcePath), token)).IsSuccess.ShouldBeTrue();
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        await Should.ThrowAsync<OperationCanceledException>(() =>
            source.MoveToAsync(SourcePath, destination, DestinationPath, cancellation.Token));

        await AssertContentAsync(source, SourcePath, "original", token);
        (await destination.ExistsAsync(DestinationPath, token)).Value.ShouldBeFalse();
    }

    private FileSystemStorage CreateStorage(string folder) =>
        new(new FileSystemStorageOptions { BaseFolder = _root.CreateSubdirectory(folder).FullName });

    private static async Task AssertContentAsync(IStorage storage, string path, string expected, CancellationToken token)
    {
        var result = await storage.GetStreamAsync(path, token);
        result.IsSuccess.ShouldBeTrue(result.Problem?.Detail);
        await using var stream = result.Value;
        using var reader = new StreamReader(stream, leaveOpen: true);
        (await reader.ReadToEndAsync(token)).ShouldBe(expected);
    }

    public void Dispose() => _root.Delete(recursive: true);
}
