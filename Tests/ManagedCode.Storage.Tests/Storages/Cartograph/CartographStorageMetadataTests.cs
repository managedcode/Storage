using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ManagedCode.Storage.Core.Models;
using Shouldly;
using Xunit;

namespace ManagedCode.Storage.Tests.Storages.Cartograph;

public sealed class CartographStorageMetadataTests
{
    [Fact]
    public async Task GetBlobMetadataAsync_WhenUsingDirectoryOptions_ReturnsCatalogMetadata()
    {
        using var fixture = new CartographArtifactFixture();
        using var storage = fixture.CreateStorage();
        var result = await storage.GetBlobMetadataAsync(new MetadataOptions { Directory = "reports/nested", FileName = "дані.txt" });
        result.IsSuccess.ShouldBeTrue();
        var metadata = result.Value.ShouldNotBeNull();
        metadata.FullName.ShouldBe("reports/nested/дані.txt");
        metadata.Name.ShouldBe("дані.txt");
        metadata.MimeType.ShouldBe("text/plain");
        metadata.Length.ShouldBe((ulong)fixture.Files[metadata.FullName].Length);
        metadata.CreatedOn.ShouldBe(new DateTimeOffset(CartographArtifactFixture.CreatedUtc));
        metadata.LastModified.ShouldBe(new DateTimeOffset(CartographArtifactFixture.ModifiedUtc));
        metadata.Container.ShouldBe(fixture.ArtifactPath);
        metadata.Uri.ShouldNotBeNull().LocalPath.ShouldBe(fixture.ArtifactPath);
        Uri.UnescapeDataString(metadata.Uri.Fragment).ShouldBe("#reports/nested/дані.txt");
    }

    [Theory]
    [InlineData(null, 5)]
    [InlineData("reports", 3)]
    [InlineData("reports/", 3)]
    [InlineData("reports\\nested", 1)]
    [InlineData("reports-old", 1)]
    [InlineData("missing", 0)]
    public async Task GetBlobMetadataListAsync_WhenDirectorySelected_ReturnsOnlyItsDescendants(string? directory, int count)
    {
        using var fixture = new CartographArtifactFixture();
        using var storage = fixture.CreateStorage();
        var entries = new List<BlobMetadata>();
        await foreach (var entry in storage.GetBlobMetadataListAsync(directory))
            entries.Add(entry);

        entries.Count.ShouldBe(count);
        entries.Select(entry => entry.FullName).Distinct().Count().ShouldBe(count);
        if (directory == "reports")
            entries.ShouldNotContain(entry => entry.FullName.StartsWith("reports-old", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData("root.txt", true)]
    [InlineData("ROOT.txt", false)]
    [InlineData("missing.txt", false)]
    [InlineData("reports\\empty.txt", true)]
    public async Task ExistsAsync_WhenPathQueried_UsesOrdinalRelativeNames(string path, bool exists)
    {
        using var fixture = new CartographArtifactFixture();
        using var storage = fixture.CreateStorage();
        var result = await storage.ExistsAsync(path);
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe(exists);
    }

    [Fact]
    public async Task GetBlobMetadataListAsync_WhenCancellationRequested_Throws()
    {
        using var fixture = new CartographArtifactFixture();
        using var storage = fixture.CreateStorage();
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        await Should.ThrowAsync<OperationCanceledException>(async () =>
        {
            await foreach (var _ in storage.GetBlobMetadataListAsync(cancellationToken: cancellation.Token)) { }
        });
    }

    [Fact]
    public async Task GetBlobMetadataListAsync_WhenOptionsChange_KeepsMetadataOnItsOriginalSnapshot()
    {
        using var fixture = new CartographArtifactFixture();
        using var storage = fixture.CreateStorage();
        await using var entries = storage.GetBlobMetadataListAsync().GetAsyncEnumerator();
        (await entries.MoveNextAsync()).ShouldBeTrue();
        var second = fixture.WriteArtifact("second.ctg");
        (await storage.SetStorageOptions(options => options.ArtifactPath = second)).IsSuccess.ShouldBeTrue();
        (await entries.MoveNextAsync()).ShouldBeTrue();
        entries.Current.Container.ShouldBe(fixture.ArtifactPath);
        entries.Current.Uri.ShouldNotBeNull().LocalPath.ShouldBe(fixture.ArtifactPath);
    }
}
