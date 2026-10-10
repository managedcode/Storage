using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using ManagedCode.Storage.Azure;
using ManagedCode.Storage.Azure.Options;
using ManagedCode.Storage.Core.Primitives;
using ManagedCode.Storage.Tests.Common;
using ManagedCode.Storage.Tests.VirtualFileSystem.Fixtures;
using ManagedCode.Storage.VirtualFileSystem.Extensions;
using ManagedCode.Storage.VirtualFileSystem.Core;
using ManagedCode.Storage.VirtualFileSystem.Options;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Shouldly;
using Testcontainers.Azurite;
using Xunit;
using VfsImplementation = ManagedCode.Storage.VirtualFileSystem.Implementations.VirtualFileSystem;

namespace ManagedCode.Storage.Tests.Storages.Azure;

public sealed class AzureObjectStorageTests : IAsyncLifetime
{
    private readonly AzuriteContainer _container = new AzuriteBuilder(ContainerImages.Azurite)
        .WithCommand("--skipApiVersionCheck").Build();

    public ValueTask InitializeAsync() => new(_container.StartAsync());
    public ValueTask DisposeAsync() => _container.DisposeAsync();

    [Fact]
    public async Task ConditionalWritesAndReads_PreserveExactRevision()
    {
        using var storage = CreateStorage();
        var objects = storage.RequireObjectStorage();
        await objects.CreatePrivateContainerAsync(new Dictionary<string, string> { ["owner"] = "first" }, TestContext.Current.CancellationToken);
        (await objects.GetContainerInfoAsync(TestContext.Current.CancellationToken)).IsPrivate.ShouldBeTrue();
        using var initial = Content("original");
        var first = await objects.WriteObjectAsync("file.txt", initial, new StorageWriteOptions { IfAbsent = true }, TestContext.Current.CancellationToken);
        using var conflict = Content("conflict");
        var duplicate = await Should.ThrowAsync<StorageOperationException>(() => objects.WriteObjectAsync("file.txt", conflict, new StorageWriteOptions { IfAbsent = true }));
        duplicate.IsConflict.ShouldBeTrue();
        using var replacement = Content("replacement");
        var second = await objects.WriteObjectAsync("file.txt", replacement, new StorageWriteOptions { IfMatch = first.ETag }, TestContext.Current.CancellationToken);
        second.ETag.ShouldNotBe(first.ETag);
        var stale = await Should.ThrowAsync<StorageOperationException>(() => objects.OpenObjectReadAsync("file.txt", new StorageReadOptions { IfMatch = first.ETag }));
        stale.IsConflict.ShouldBeTrue();
        await using var stream = await objects.OpenObjectReadAsync("file.txt", new StorageReadOptions { IfMatch = second.ETag, Offset = 2, Length = 4 }, TestContext.Current.CancellationToken);
        using var reader = new StreamReader(stream);
        (await reader.ReadToEndAsync(TestContext.Current.CancellationToken)).ShouldBe("plac");
        using var rejected = Content("wrong");
        (await Should.ThrowAsync<StorageOperationException>(() => objects.WriteObjectAsync("file.txt", rejected, new StorageWriteOptions { IfMatch = first.ETag }))).IsConflict.ShouldBeTrue();
        (await objects.GetObjectInfoAsync("file.txt", TestContext.Current.CancellationToken)).Length.ShouldBe(11);
    }

    [Fact]
    public async Task MultipartUpload_ResumesAndCommitsInRequestedOrder()
    {
        using var storage = CreateStorage();
        var multipart = storage.RequireMultipartStorage();
        await multipart.CreatePrivateContainerAsync(cancellationToken: TestContext.Current.CancellationToken);
        var firstId = Convert.ToBase64String(Encoding.UTF8.GetBytes("0001"));
        var secondId = Convert.ToBase64String(Encoding.UTF8.GetBytes("0002"));
        using var first = Content("first");
        await multipart.StagePartAsync("archive", firstId, first, TestContext.Current.CancellationToken);
        using var resumed = CreateStorage(storage.ContainerUri.Segments[^1]);
        var resumedParts = resumed.RequireMultipartStorage();
        using var second = Content("second");
        await resumedParts.StagePartAsync("archive", secondId, second, TestContext.Current.CancellationToken);
        var committed = await resumedParts.CommitPartsAsync("archive", [secondId, firstId], new StorageWriteOptions { IfAbsent = true, ContentType = "application/zip" }, TestContext.Current.CancellationToken);
        committed.Length.ShouldBe(11);
        committed.ContentType.ShouldBe("application/zip");
        await using var stream = await resumedParts.OpenObjectReadAsync("archive", cancellationToken: TestContext.Current.CancellationToken);
        using var reader = new StreamReader(stream);
        (await reader.ReadToEndAsync(TestContext.Current.CancellationToken)).ShouldBe("secondfirst");
        (await Should.ThrowAsync<StorageOperationException>(() => resumedParts.CommitPartsAsync("archive", [firstId], new StorageWriteOptions { IfAbsent = true }))).IsConflict.ShouldBeTrue();
    }

    [Fact]
    public async Task VerifiedUpload_RetryWithSameContentReusesRevision_AndDifferentContentConflicts()
    {
        using var storage = CreateStorage();
        var objects = storage.RequireObjectStorage();
        await objects.CreatePrivateContainerAsync(cancellationToken: TestContext.Current.CancellationToken);
        var metadata = new Dictionary<string, string>
        {
            ["owner"] = "company-a",
            ["generation"] = "1"
        };
        using var firstContent = Content("original");
        var first = await objects.WriteIfAbsentOrSameAsync("file.txt", firstContent, 8, new StorageWriteOptions { ContentType = "text/plain", Metadata = metadata }, cancellationToken: TestContext.Current.CancellationToken);
        first.ReusedExisting.ShouldBeFalse();
        using var retryContent = Content("original");
        var retry = await objects.WriteIfAbsentOrSameAsync("file.txt", retryContent, 8, new StorageWriteOptions { ContentType = "text/plain", Metadata = metadata }, cancellationToken: TestContext.Current.CancellationToken);
        retry.ReusedExisting.ShouldBeTrue();
        retry.Info.ETag.ShouldBe(first.Info.ETag);
        retry.Sha256.ShouldBe(first.Sha256);
        using var differentlyCasedMetadata = Content("original");
        var sameMetadata = await objects.WriteIfAbsentOrSameAsync("file.txt", differentlyCasedMetadata, 8, new StorageWriteOptions
        {
            ContentType = "text/plain",
            Metadata = new Dictionary<string, string>
            {
                ["Owner"] = "company-a",
                ["Generation"] = "1"
            }
        }, cancellationToken: TestContext.Current.CancellationToken);
        sameMetadata.ReusedExisting.ShouldBeTrue();
        sameMetadata.Info.ETag.ShouldBe(first.Info.ETag);
        using var wrongType = Content("original");
        (await Should.ThrowAsync<StorageOperationException>(() =>
            objects.WriteIfAbsentOrSameAsync("file.txt", wrongType, 8,
                new StorageWriteOptions { ContentType = "application/json" }))).IsConflict.ShouldBeTrue();
        using var wrongOwner = Content("original");
        (await Should.ThrowAsync<StorageOperationException>(() =>
            objects.WriteIfAbsentOrSameAsync("file.txt", wrongOwner, 8,
                new StorageWriteOptions
                {
                    ContentType = "text/plain",
                    Metadata = new Dictionary<string, string>
                    {
                        ["owner"] = "company-b",
                        ["generation"] = "1"
                    }
                }))).IsConflict.ShouldBeTrue();
        using var wrongMetadataCount = Content("original");
        (await Should.ThrowAsync<StorageOperationException>(() =>
            objects.WriteIfAbsentOrSameAsync("file.txt", wrongMetadataCount, 8,
                new StorageWriteOptions
                {
                    Metadata = new Dictionary<string, string> { ["owner"] = "company-a" }
                }))).IsConflict.ShouldBeTrue();
        using var wrongMetadataKey = Content("original");
        (await Should.ThrowAsync<StorageOperationException>(() =>
            objects.WriteIfAbsentOrSameAsync("file.txt", wrongMetadataKey, 8,
                new StorageWriteOptions
                {
                    Metadata = new Dictionary<string, string>
                    {
                        ["tenant"] = "company-a",
                        ["generation"] = "1"
                    }
                }))).IsConflict.ShouldBeTrue();
        using var wrongEncoding = Content("original");
        (await Should.ThrowAsync<StorageOperationException>(() =>
            objects.WriteIfAbsentOrSameAsync("file.txt", wrongEncoding, 8,
                new StorageWriteOptions { ContentEncoding = "gzip" }))).IsConflict.ShouldBeTrue();
        using var differentContent = Content("altered!");
        (await Should.ThrowAsync<StorageOperationException>(() =>
            objects.WriteIfAbsentOrSameAsync("file.txt", differentContent, 8))).IsConflict.ShouldBeTrue();
        await using var stored = await objects.OpenObjectReadAsync("file.txt", cancellationToken: TestContext.Current.CancellationToken);
        using var reader = new StreamReader(stored);
        (await reader.ReadToEndAsync(TestContext.Current.CancellationToken)).ShouldBe("original");
    }

    [Fact]
    public async Task VerifiedUpload_DeclaredLengthMismatchFailsWithoutAcceptingContent()
    {
        using var storage = CreateStorage();
        var objects = storage.RequireObjectStorage();
        await objects.CreatePrivateContainerAsync(cancellationToken: TestContext.Current.CancellationToken);
        using var tooLong = Content("too long");
        (await Should.ThrowAsync<StorageUploadLengthException>(() =>
            objects.WriteIfAbsentOrSameAsync("too-long.txt", tooLong, 3))).TooLarge.ShouldBeTrue();
        using var tooShort = Content("short");
        (await Should.ThrowAsync<StorageUploadLengthException>(() =>
            objects.WriteIfAbsentOrSameAsync("too-short.txt", tooShort, 9))).TooLarge.ShouldBeFalse();
        (await objects.ObjectExistsAsync("too-long.txt", TestContext.Current.CancellationToken)).ShouldBeFalse();
        (await objects.ObjectExistsAsync("too-short.txt", TestContext.Current.CancellationToken)).ShouldBeFalse();
    }

    [Fact]
    public async Task ImmutableVfsBytes_AreAtomicAndInvalidateExistenceCache()
    {
        using var storage = CreateStorage();
        await storage.RequireObjectStorage().CreatePrivateContainerAsync(cancellationToken: TestContext.Current.CancellationToken);
        using var cache = new MemoryCache(new MemoryCacheOptions());
        await using var fileSystem = new VfsImplementation(storage,
            new TestMetadataManager(storage), Options.Create(new VfsOptions { EnableCache = true }),
            cache, NullLogger<VfsImplementation>.Instance);
        var path = new VfsPath("/commands/payload.json");
        (await fileSystem.FileExistsAsync(path, TestContext.Current.CancellationToken)).ShouldBeFalse();
        var metadata = new Dictionary<string, string> { ["owner"] = "one" };
        var options = new StorageWriteOptions { ContentType = "application/json", Metadata = metadata };
        var bytes = Encoding.UTF8.GetBytes("{\"ok\":true}");

        var first = await fileSystem.WriteBytesIfAbsentOrSameAsync(path, bytes, options, TestContext.Current.CancellationToken);
        first.ReusedExisting.ShouldBeFalse();
        (await fileSystem.FileExistsAsync(path, TestContext.Current.CancellationToken)).ShouldBeTrue();

        var retry = await fileSystem.WriteBytesIfAbsentOrSameAsync(path, bytes, options, TestContext.Current.CancellationToken);
        retry.ReusedExisting.ShouldBeTrue();
        retry.Info.ETag.ShouldBe(first.Info.ETag);
        (await Should.ThrowAsync<StorageOperationException>(() =>
            fileSystem.WriteBytesIfAbsentOrSameAsync(path,
                Encoding.UTF8.GetBytes("{\"ok\":false}"), options))).IsConflict.ShouldBeTrue();
        (await Should.ThrowAsync<StorageOperationException>(() =>
            fileSystem.WriteBytesIfAbsentOrSameAsync(path, bytes,
                options with { Metadata = new Dictionary<string, string> { ["owner"] = "two" } })))
            .IsConflict.ShouldBeTrue();
    }

    [Fact]
    public async Task MetadataPaginationAndDeletion_PreserveContainerBoundary()
    {
        using var storage = CreateStorage();
        var objects = storage.RequireObjectStorage();
        await objects.CreatePrivateContainerAsync(new Dictionary<string, string> { ["owner"] = "one" }, TestContext.Current.CancellationToken);
        await objects.SetContainerMetadataAsync(new Dictionary<string, string> { ["owner"] = "two" }, TestContext.Current.CancellationToken);
        for (var index = 0; index < 3; index++)
        {
            using var content = Content("data");
            await objects.WriteObjectAsync($"files/{index}", content, cancellationToken: TestContext.Current.CancellationToken);
        }
        var page = await objects.ListObjectsAsync("files/", pageSize: 2, cancellationToken: TestContext.Current.CancellationToken);
        page.Items.Count.ShouldBe(2);
        page.ContinuationToken.ShouldNotBeNullOrEmpty();
        var next = await objects.ListObjectsAsync("files/", page.ContinuationToken, 2, TestContext.Current.CancellationToken);
        next.Items.Count.ShouldBe(1);
        next.Items[0].Path.ShouldBe("files/2");
        next.ContinuationToken.ShouldBeNull();
        (await objects.ListObjectsAsync("files/", pageSize: 1000, cancellationToken: TestContext.Current.CancellationToken)).ContinuationToken.ShouldBeNull();
        (await objects.ListObjectsAsync("absent/", cancellationToken: TestContext.Current.CancellationToken)).ContinuationToken.ShouldBeNull();
        var info = next.Items[0];
        (await objects.GetObjectInfoAsync(info.Path, TestContext.Current.CancellationToken)).ETag.ShouldBe(info.ETag);
        await objects.SetObjectMetadataAsync(info.Path, new Dictionary<string, string> { ["hash"] = "value" }, info.ETag, TestContext.Current.CancellationToken);
        (await objects.GetObjectInfoAsync(info.Path, TestContext.Current.CancellationToken)).Metadata["hash"].ShouldBe("value");
        (await Should.ThrowAsync<StorageOperationException>(() => objects.SetObjectMetadataAsync(info.Path, new Dictionary<string, string>(), info.ETag))).IsConflict.ShouldBeTrue();
        (await objects.DeleteObjectIfExistsAsync(info.Path, includeSnapshots: true, cancellationToken: TestContext.Current.CancellationToken)).ShouldBeTrue();
        (await objects.ObjectExistsAsync(info.Path, TestContext.Current.CancellationToken)).ShouldBeFalse();
        (await Should.ThrowAsync<StorageOperationException>(() => objects.GetObjectInfoAsync(info.Path))).IsNotFound.ShouldBeTrue();
        (await objects.GetContainerInfoAsync(TestContext.Current.CancellationToken)).Metadata["owner"].ShouldBe("two");
        (await objects.DeleteContainerIfExistsAsync(TestContext.Current.CancellationToken)).ShouldBeTrue();
        (await objects.DeleteContainerIfExistsAsync(TestContext.Current.CancellationToken)).ShouldBeFalse();
    }

    [Fact]
    public async Task VfsEmptyWrites_CreateAndTruncateStoredFiles()
    {
        using var storage = CreateStorage();
        await storage.RequireObjectStorage().CreatePrivateContainerAsync(cancellationToken: TestContext.Current.CancellationToken);
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddVirtualFileSystem(storage, options => options.EnableCache = false);
        await using var provider = services.BuildServiceProvider();
        var vfs = provider.GetRequiredService<IVirtualFileSystem>();
        var empty = await vfs.GetFileAsync(new VfsPath("/empty.txt"), TestContext.Current.CancellationToken);
        await empty.WriteAllBytesAsync([], cancellationToken: TestContext.Current.CancellationToken);
        (await storage.RequireObjectStorage().GetObjectInfoAsync("empty.txt", TestContext.Current.CancellationToken)).Length.ShouldBe(0);
        await using (var emptyRead = await storage.RequireObjectStorage().OpenObjectReadAsync("empty.txt", cancellationToken: TestContext.Current.CancellationToken))
        {
            (await emptyRead.ReadAsync(new byte[1], TestContext.Current.CancellationToken)).ShouldBe(0);
        }
        var populated = await vfs.GetFileAsync(new VfsPath("/populated.txt"), TestContext.Current.CancellationToken);
        await populated.WriteAllTextAsync("previous", cancellationToken: TestContext.Current.CancellationToken);
        await populated.WriteAllBytesAsync([], new ManagedCode.Storage.VirtualFileSystem.Options.WriteOptions { Overwrite = true }, TestContext.Current.CancellationToken);
        (await storage.RequireObjectStorage().GetObjectInfoAsync("populated.txt", TestContext.Current.CancellationToken)).Length.ShouldBe(0);
    }

    private AzureStorage CreateStorage(string? name = null) => new(new AzureStorageOptions
    {
        ConnectionString = _container.GetConnectionString(),
        Container = name ?? $"objects-{Guid.NewGuid():N}",
        CreateContainerIfNotExists = false
    });

    private static MemoryStream Content(string text) => new(Encoding.UTF8.GetBytes(text));
}
