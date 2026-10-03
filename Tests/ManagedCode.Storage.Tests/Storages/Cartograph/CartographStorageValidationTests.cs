using System;
using System.IO;
using System.Threading.Tasks;
using Cartograph.Format;
using ManagedCode.Storage.Core.Models;
using Shouldly;
using Xunit;

namespace ManagedCode.Storage.Tests.Storages.Cartograph;

public sealed class CartographStorageValidationTests
{
    [Theory]
    [InlineData("../root.txt")]
    [InlineData("/root.txt")]
    [InlineData("C:\\root.txt")]
    [InlineData("reports/../root.txt")]
    [InlineData("reports//empty.txt")]
    [InlineData("")]
    public async Task GetStreamAsync_WhenPathIsUnsafe_ReturnsFailure(string path)
    {
        using var fixture = new CartographArtifactFixture();
        using var storage = fixture.CreateStorage();
        (await storage.GetStreamAsync(path)).IsFailed.ShouldBeTrue();
        (await storage.ExistsAsync(path)).IsFailed.ShouldBeTrue();
    }

    [Fact]
    public async Task ReadOperations_WhenFileMissing_ReportFailureOrFalseExistence()
    {
        using var fixture = new CartographArtifactFixture();
        using var storage = fixture.CreateStorage();
        (await storage.GetStreamAsync("missing.txt")).Problem.ShouldNotBeNull().Title.ShouldBe(nameof(FileNotFoundException));
        (await storage.DownloadAsync("missing.txt")).IsFailed.ShouldBeTrue();
        (await storage.GetBlobMetadataAsync("missing.txt")).IsFailed.ShouldBeTrue();
        (await storage.ExistsAsync("missing.txt")).Value.ShouldBeFalse();
    }

    [Theory]
    [InlineData("traversal")]
    [InlineData("backslash")]
    [InlineData("negative-length")]
    [InlineData("wrong-length")]
    [InlineData("catalog-segment")]
    [InlineData("missing-segment")]
    [InlineData("negative-record")]
    [InlineData("wrong-global")]
    [InlineData("empty-span")]
    [InlineData("outside-span")]
    [InlineData("invalid-time")]
    [InlineData("duplicate-path")]
    public async Task CreateContainerAsync_WhenCatalogIsInvalid_RejectsArtifact(string defect)
    {
        using var fixture = new CartographArtifactFixture();
        var path = fixture.WriteArtifact("invalid.ctg", entries =>
        {
            var entry = entries[0];
            entries[0] = CartographArtifactFixture.Change(entry,
                path: defect == "traversal" ? "../root.txt" : defect == "backslash" ? "reports\\root.txt" : null,
                length: defect == "negative-length" ? -1 : defect == "wrong-length" ? entry.Length + 1 : null,
                segment: defect == "catalog-segment" ? 0 : defect == "missing-segment" ? 99 : null,
                record: defect == "negative-record" ? -1 : null,
                global: defect == "wrong-global" ? 2 : null,
                count: defect == "empty-span" ? 0 : defect == "outside-span" ? int.MaxValue : null,
                ticks: defect == "invalid-time" ? -1 : null);
            if (defect == "duplicate-path")
                entries.Add(entry);
        });
        using var storage = fixture.CreateStorage(path);
        (await storage.CreateContainerAsync()).IsFailed.ShouldBeTrue();
        (await storage.GetStreamAsync("root.txt")).IsFailed.ShouldBeTrue();
    }

    [Fact]
    public async Task GetStreamAsync_WhenPayloadCorrupted_DefersChecksumFailureUntilRead()
    {
        using var fixture = new CartographArtifactFixture();
        fixture.CorruptFirstPayload();
        using var storage = fixture.CreateStorage();
        (await storage.GetBlobMetadataAsync("root.txt")).IsSuccess.ShouldBeTrue();
        var result = await storage.GetStreamAsync("root.txt");
        result.IsSuccess.ShouldBeTrue();
        await using var stream = result.Value.ShouldNotBeNull();
        await Should.ThrowAsync<CartographFormatException>(async () => await stream.ReadExactlyAsync(new byte[1]));
        (await storage.DownloadAsync("root.txt")).IsFailed.ShouldBeTrue();
    }

    [Fact]
    public async Task CreateContainerAsync_WhenArtifactMissingOrMalformed_NeverCreatesIt()
    {
        using var fixture = new CartographArtifactFixture();
        var missing = fixture.ArtifactPath + ".missing";
        using var storage = fixture.CreateStorage(missing);
        (await storage.CreateContainerAsync()).IsFailed.ShouldBeTrue();
        File.Exists(missing).ShouldBeFalse();
        await File.WriteAllTextAsync(missing, "not a Cartograph artifact");
        (await storage.CreateContainerAsync()).IsFailed.ShouldBeTrue();
        (await File.ReadAllTextAsync(missing)).ShouldBe("not a Cartograph artifact");
    }

    [Fact]
    public async Task DownloadAsync_WhenDestinationIsArtifact_LeavesSourceUnchanged()
    {
        using var fixture = new CartographArtifactFixture();
        var original = await File.ReadAllBytesAsync(fixture.ArtifactPath);
        using var storage = fixture.CreateStorage();
        var result = await storage.DownloadAsync(new DownloadOptions { FileName = "root.txt", LocalPath = fixture.ArtifactPath });
        result.IsFailed.ShouldBeTrue();
        (await File.ReadAllBytesAsync(fixture.ArtifactPath)).ShouldBe(original);
    }

    [Fact]
    public async Task MutationOperations_WhenCalled_ReturnUnsupportedAndPreserveArtifact()
    {
        using var fixture = new CartographArtifactFixture();
        var original = await File.ReadAllBytesAsync(fixture.ArtifactPath);
        using var storage = fixture.CreateStorage();
        (await storage.UploadAsync("replacement", new UploadOptions { FileName = "root.txt" })).Problem.ShouldNotBeNull().Title.ShouldBe(nameof(NotSupportedException));
        (await storage.DeleteAsync("root.txt")).Problem.ShouldNotBeNull().Title.ShouldBe(nameof(NotSupportedException));
        (await storage.DeleteDirectoryAsync("reports")).Problem.ShouldNotBeNull().Title.ShouldBe(nameof(NotSupportedException));
        (await storage.RemoveContainerAsync()).Problem.ShouldNotBeNull().Title.ShouldBe(nameof(NotSupportedException));
        (await storage.SetLegalHoldAsync(true, "root.txt")).Problem.ShouldNotBeNull().Title.ShouldBe(nameof(NotSupportedException));
        (await storage.HasLegalHoldAsync("root.txt")).Problem.ShouldNotBeNull().Title.ShouldBe(nameof(NotSupportedException));
        (await File.ReadAllBytesAsync(fixture.ArtifactPath)).ShouldBe(original);
    }
}
