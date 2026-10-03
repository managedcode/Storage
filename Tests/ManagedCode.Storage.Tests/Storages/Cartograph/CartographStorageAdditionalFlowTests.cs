using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Cartograph.Format;
using ManagedCode.Storage.Cartograph.Options;
using ManagedCode.Storage.Core.Models;
using Shouldly;
using Xunit;

namespace ManagedCode.Storage.Tests.Storages.Cartograph;

public sealed class CartographStorageAdditionalFlowTests
{
    [Fact]
    public async Task SetStorageOptions_WhenStreamIsOpen_KeepsItsOriginalSnapshot()
    {
        using var fixture = new CartographArtifactFixture();
        using var storage = fixture.CreateStorage();
        await using var original = (await storage.GetStreamAsync("root.txt")).Value.ShouldNotBeNull();
        var second = fixture.WriteArtifact("second.ctg", entries => entries[0] = CartographArtifactFixture.Change(entries[0], path: "changed.txt"));
        (await storage.SetStorageOptions(options => options.ArtifactPath = second)).IsSuccess.ShouldBeTrue();
        (await storage.ExistsAsync("root.txt")).Value.ShouldBeFalse();
        (await storage.ExistsAsync("changed.txt")).Value.ShouldBeTrue();
        var bytes = new byte[fixture.Files["root.txt"].Length];
        await original.ReadExactlyAsync(bytes);
        bytes.ShouldBe(fixture.Files["root.txt"]);
    }

    [Fact]
    public async Task GetStreamAsync_WhenArrayReadUsed_HonorsOffsetsAndEmptyBuffers()
    {
        using var fixture = new CartographArtifactFixture();
        using var storage = fixture.CreateStorage();
        await using var stream = (await storage.GetStreamAsync("root.txt")).Value.ShouldNotBeNull();
        stream.Read(Span<byte>.Empty).ShouldBe(0);
        (await stream.ReadAsync(Memory<byte>.Empty)).ShouldBe(0);
        var buffer = new byte[8];
        var count = await stream.ReadAsync(buffer, 2, 3, CancellationToken.None);
        count.ShouldBe(3);
        buffer.AsSpan(2, 3).ToArray().ShouldBe(fixture.Files["root.txt"].AsSpan(0, 3).ToArray());
        buffer[0].ShouldBe((byte)0);
        stream.Position.ShouldBe(3);
        await stream.FlushAsync();
    }

    [Fact]
    public async Task ReadOperations_WhenCancelled_DoNotCreateSuccessfulResults()
    {
        using var fixture = new CartographArtifactFixture();
        using var storage = fixture.CreateStorage();
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        (await storage.ExistsAsync("root.txt", cancellation.Token)).IsFailed.ShouldBeTrue();
        (await storage.GetBlobMetadataAsync("root.txt", cancellation.Token)).IsFailed.ShouldBeTrue();
        (await storage.DownloadAsync("root.txt", cancellation.Token)).IsFailed.ShouldBeTrue();
    }

    [Fact]
    public async Task CreateContainerAsync_WhenCatalogIsEmpty_AllowsAnEmptyFileCollection()
    {
        using var fixture = new CartographArtifactFixture();
        var path = fixture.WriteArtifact("empty.ctg", entries => entries.Clear());
        using var storage = fixture.CreateStorage(path);
        (await storage.CreateContainerAsync()).IsSuccess.ShouldBeTrue();
        (await storage.ExistsAsync("root.txt")).Value.ShouldBeFalse();
        var entries = new List<BlobMetadata>();
        await foreach (var metadata in storage.GetBlobMetadataListAsync())
            entries.Add(metadata);
        entries.ShouldBeEmpty();
    }

    [Fact]
    public async Task CreateContainerAsync_WhenArtifactHasNoCatalog_RejectsIt()
    {
        using var fixture = new CartographArtifactFixture();
        var writer = new SegmentedArtifactWriter();
        writer.AddSegment().AddRecord("arbitrary record bytes"u8);
        writer.Save(fixture.DestinationPath);
        using var storage = fixture.CreateStorage(fixture.DestinationPath);
        (await storage.CreateContainerAsync()).IsFailed.ShouldBeTrue();
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task DownloadAsync_WhenPathsLinkToArtifact_LeavesSourceUnchanged(bool destinationIsLink)
    {
        using var fixture = new CartographArtifactFixture();
        File.CreateSymbolicLink(fixture.DestinationPath, fixture.ArtifactPath);
        var original = await File.ReadAllBytesAsync(fixture.ArtifactPath);
        using var storage = fixture.CreateStorage(destinationIsLink ? fixture.ArtifactPath : fixture.DestinationPath);
        var result = await storage.DownloadAsync(new DownloadOptions
        {
            FileName = "root.txt",
            LocalPath = destinationIsLink ? fixture.DestinationPath : fixture.ArtifactPath
        });
        result.IsFailed.ShouldBeTrue();
        (await File.ReadAllBytesAsync(fixture.ArtifactPath)).ShouldBe(original);
    }

    [Fact]
    public async Task DownloadAsync_WhenPayloadCorrupted_PreservesExistingDestination()
    {
        using var fixture = new CartographArtifactFixture();
        fixture.CorruptFirstPayload();
        await File.WriteAllTextAsync(fixture.DestinationPath, "existing destination");
        using var storage = fixture.CreateStorage();
        var result = await storage.DownloadAsync(new DownloadOptions { FileName = "root.txt", LocalPath = fixture.DestinationPath });
        result.IsFailed.ShouldBeTrue();
        (await File.ReadAllTextAsync(fixture.DestinationPath)).ShouldBe("existing destination");
    }
}
