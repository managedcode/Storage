using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Azure.Storage.Blobs;
using ManagedCode.Storage.Azure;
using ManagedCode.Storage.Azure.Options;
using ManagedCode.Storage.Core.Models;
using ManagedCode.Storage.Core.Primitives;
using ManagedCode.Storage.Tests.Common;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;
using Testcontainers.Azurite;
using Xunit;

namespace ManagedCode.Storage.Tests.Storages.Azure;

public sealed class AzureMetadataEncodingTests : IAsyncLifetime
{
    private readonly AzuriteContainer _container = new AzuriteBuilder(ContainerImages.Azurite)
        .WithCommand("--skipApiVersionCheck").Build();

    public Task InitializeAsync() => _container.StartAsync();
    public Task DisposeAsync() => _container.DisposeAsync().AsTask();

    [Fact]
    public async Task PortableUpload_PreservesUnicodeMetadataAcrossReadsAndDownload()
    {
        using var storage = CreateStorage();
        var metadata = UnicodeMetadata();
        using var content = Content("unchanged material");
        var upload = await storage.UploadAsync(content, new UploadOptions
        {
            FileName = "material.txt",
            MimeType = "text/plain",
            Metadata = metadata
        });
        upload.IsSuccess.ShouldBeTrue();
        AssertMetadata(upload.Value!.Metadata!, metadata);
        var stored = await storage.GetBlobMetadataAsync("material.txt");
        stored.IsSuccess.ShouldBeTrue();
        AssertMetadata(stored.Value!.Metadata!, metadata);
        var listed = await storage.GetBlobMetadataListAsync().ToListAsync();
        AssertMetadata(listed.Single().Metadata!, metadata);
        var downloaded = await storage.DownloadAsync("material.txt");
        downloaded.IsSuccess.ShouldBeTrue();
        using var file = downloaded.Value!;
        AssertMetadata(file.BlobMetadata!.Metadata!, metadata);
        (await File.ReadAllTextAsync(file.FileInfo.FullName)).ShouldBe("unchanged material");
        var native = await NativeContainer(storage).GetBlobClient("material.txt").GetPropertiesAsync();
        native.Value.Metadata.Values.All(value => value.All(c => c is >= ' ' and <= '~')).ShouldBeTrue();
    }

    [Fact]
    public async Task ObjectAndContainerMetadata_PreserveValuesAndConditionalRevision()
    {
        using var storage = CreateStorage();
        var objects = storage.RequireObjectStorage();
        var metadata = UnicodeMetadata();
        await objects.CreatePrivateContainerAsync(metadata);
        AssertMetadata((await objects.GetContainerInfoAsync()).Metadata, metadata);
        var replacement = new Dictionary<string, string> { ["owner"] = "Компанія" };
        await objects.SetContainerMetadataAsync(replacement);
        AssertMetadata((await objects.GetContainerInfoAsync()).Metadata, replacement);
        using var content = Content("unchanged evidence");
        var first = await objects.WriteIfAbsentOrSameAsync("evidence", content, content.Length,
            new StorageWriteOptions { ContentType = "text/plain", Metadata = metadata });
        AssertMetadata(first.Info.Metadata, metadata);
        using var retry = Content("unchanged evidence");
        var reused = await objects.WriteIfAbsentOrSameAsync("evidence", retry, retry.Length,
            new StorageWriteOptions { ContentType = "text/plain", Metadata = metadata });
        reused.ReusedExisting.ShouldBeTrue();
        reused.Info.ETag.ShouldBe(first.Info.ETag);
        AssertMetadata((await objects.ListObjectsAsync()).Items.Single().Metadata, metadata);
        await objects.SetObjectMetadataAsync("evidence", replacement, first.Info.ETag);
        var second = await objects.GetObjectInfoAsync("evidence");
        AssertMetadata(second.Metadata, replacement);
        second.ETag.ShouldNotBe(first.Info.ETag);
        var stale = await Should.ThrowAsync<StorageOperationException>(() =>
            objects.SetObjectMetadataAsync("evidence", metadata, first.Info.ETag));
        stale.IsConflict.ShouldBeTrue();
        await using var stream = await objects.OpenObjectReadAsync("evidence", new StorageReadOptions { IfMatch = second.ETag });
        using var reader = new StreamReader(stream);
        (await reader.ReadToEndAsync()).ShouldBe("unchanged evidence");
    }

    [Fact]
    public async Task MultipartAndMarkerCollision_PreserveCompleteLogicalMetadata()
    {
        using var storage = CreateStorage();
        var objects = storage.RequireMultipartStorage();
        await objects.CreatePrivateContainerAsync();
        var metadata = UnicodeMetadata();
        var partId = Convert.ToBase64String(Encoding.UTF8.GetBytes("0001"));
        using var content = Content("multipart material");
        await objects.StagePartAsync("multipart", partId, content);
        var committed = await objects.CommitPartsAsync("multipart", [partId], new StorageWriteOptions { Metadata = metadata });
        AssertMetadata(committed.Metadata, metadata);
        var native = await NativeContainer(storage).GetBlobClient("multipart").GetPropertiesAsync();
        var marker = native.Value.Metadata.Single();
        var collision = new Dictionary<string, string> { [marker.Key] = marker.Value };
        using var collidingContent = Content("caller marker");
        await objects.WriteObjectAsync("collision", collidingContent, new StorageWriteOptions { Metadata = collision });
        AssertMetadata((await objects.GetObjectInfoAsync("collision")).Metadata, collision);
        await using var stream = await objects.OpenObjectReadAsync("multipart");
        using var reader = new StreamReader(stream);
        (await reader.ReadToEndAsync()).ShouldBe("multipart material");
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task MalformedEnvelope_FailsWithoutReturningTransportMetadata(bool mixedMetadata)
    {
        using var storage = CreateStorage();
        var objects = storage.RequireObjectStorage();
        await objects.CreatePrivateContainerAsync();
        using var content = Content("material");
        await objects.WriteObjectAsync("invalid", content, new StorageWriteOptions { Metadata = UnicodeMetadata() });
        var blob = NativeContainer(storage).GetBlobClient("invalid");
        var encoded = (await blob.GetPropertiesAsync()).Value.Metadata.Single();
        var prefix = encoded.Value[..(encoded.Value.IndexOf(':') + 1)];
        var invalid = new Dictionary<string, string>
        {
            [encoded.Key] = mixedMetadata ? encoded.Value : prefix + "invalid-base64!"
        };
        if (mixedMetadata)
            invalid["unexpected"] = "sibling";
        await blob.SetMetadataAsync(invalid);
        await Should.ThrowAsync<InvalidDataException>(() => objects.GetObjectInfoAsync("invalid"));
        (await storage.GetBlobMetadataAsync("invalid")).IsFailed.ShouldBeTrue();
    }

    [Fact]
    public async Task NativeAsciiMetadata_RemainsUnencodedAndUninterpreted()
    {
        using var storage = CreateStorage();
        var objects = storage.RequireObjectStorage();
        await objects.CreatePrivateContainerAsync();
        var metadata = new Dictionary<string, string>
        {
            ["name"] = "%D0%BC.txt",
            ["literal"] = "utf8-json-base64:eyJmb28iOiJiYXIifQ=="
        };
        using var nativeContent = Content("native bytes");
        await NativeContainer(storage).GetBlobClient("native").UploadAsync(nativeContent,
            new global::Azure.Storage.Blobs.Models.BlobUploadOptions { Metadata = metadata });
        AssertMetadata((await objects.GetObjectInfoAsync("native")).Metadata, metadata);
        using var content = Content("provider bytes");
        await objects.WriteObjectAsync("provider", content, new StorageWriteOptions { Metadata = metadata });
        var native = await NativeContainer(storage).GetBlobClient("provider").GetPropertiesAsync();
        AssertMetadata(native.Value.Metadata, metadata);
    }

    private AzureStorage CreateStorage() => new(new AzureStorageOptions
    {
        ConnectionString = _container.GetConnectionString(),
        Container = $"metadata-{Guid.NewGuid():N}"
    }, NullLogger<AzureStorage>.Instance);

    private BlobContainerClient NativeContainer(AzureStorage storage) =>
        new(_container.GetConnectionString(), storage.ContainerUri.Segments[^1]);

    private static MemoryStream Content(string value) => new(Encoding.UTF8.GetBytes(value));

    private static Dictionary<string, string> UnicodeMetadata() => new()
    {
        ["fileName"] = "матеріал 日本語 😀.txt",
        ["notes"] = "line one\nline two\t"
    };

    private static void AssertMetadata(IEnumerable<KeyValuePair<string, string>> actual,
        IReadOnlyDictionary<string, string> expected)
    {
        var values = actual.ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.OrdinalIgnoreCase);
        values.Count.ShouldBe(expected.Count);
        foreach (var pair in expected)
            values[pair.Key].ShouldBe(pair.Value);
    }
}
