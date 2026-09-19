using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using ManagedCode.Storage.Azure;
using ManagedCode.Storage.Azure.Options;
using ManagedCode.Storage.Core.Objects;
using ManagedCode.Storage.Tests.Common;
using ManagedCode.Storage.VirtualFileSystem.Extensions;
using ManagedCode.Storage.VirtualFileSystem.Core;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Testcontainers.Azurite;
using Xunit;

namespace ManagedCode.Storage.Tests.Storages.Azure;

public sealed class AzureObjectStorageTests : IAsyncLifetime
{
    private readonly AzuriteContainer _container = new AzuriteBuilder(ContainerImages.Azurite)
        .WithCommand("--skipApiVersionCheck").Build();

    public Task InitializeAsync() => _container.StartAsync();
    public Task DisposeAsync() => _container.DisposeAsync().AsTask();

    [Fact]
    public async Task ConditionalWritesAndReads_PreserveExactRevision()
    {
        using var storage = CreateStorage();
        var objects = storage.RequireObjectStorage();
        await objects.CreatePrivateContainerAsync(new Dictionary<string, string> { ["owner"] = "first" });
        (await objects.GetContainerInfoAsync()).IsPrivate.ShouldBeTrue();
        using var initial = Content("original");
        var first = await objects.WriteObjectAsync("file.txt", initial, new StorageWriteOptions { IfAbsent = true });
        using var conflict = Content("conflict");
        var duplicate = await Should.ThrowAsync<StorageOperationException>(() => objects.WriteObjectAsync("file.txt", conflict, new StorageWriteOptions { IfAbsent = true }));
        duplicate.IsConflict.ShouldBeTrue();
        using var replacement = Content("replacement");
        var second = await objects.WriteObjectAsync("file.txt", replacement, new StorageWriteOptions { IfMatch = first.ETag });
        second.ETag.ShouldNotBe(first.ETag);
        var stale = await Should.ThrowAsync<StorageOperationException>(() => objects.OpenObjectReadAsync("file.txt", new StorageReadOptions { IfMatch = first.ETag }));
        stale.IsConflict.ShouldBeTrue();
        await using var stream = await objects.OpenObjectReadAsync("file.txt", new StorageReadOptions { IfMatch = second.ETag, Offset = 2, Length = 4 });
        using var reader = new StreamReader(stream);
        (await reader.ReadToEndAsync()).ShouldBe("plac");
        using var rejected = Content("wrong");
        (await Should.ThrowAsync<StorageOperationException>(() => objects.WriteObjectAsync("file.txt", rejected, new StorageWriteOptions { IfMatch = first.ETag }))).IsConflict.ShouldBeTrue();
        (await objects.GetObjectInfoAsync("file.txt")).Length.ShouldBe(11);
    }

    [Fact]
    public async Task MultipartUpload_ResumesAndCommitsInRequestedOrder()
    {
        using var storage = CreateStorage();
        var multipart = storage.RequireMultipartStorage();
        await multipart.CreatePrivateContainerAsync();
        var firstId = Convert.ToBase64String(Encoding.UTF8.GetBytes("0001"));
        var secondId = Convert.ToBase64String(Encoding.UTF8.GetBytes("0002"));
        using var first = Content("first");
        await multipart.StagePartAsync("archive", firstId, first);
        using var resumed = CreateStorage(storage.ContainerUri.Segments[^1]);
        var resumedParts = resumed.RequireMultipartStorage();
        using var second = Content("second");
        await resumedParts.StagePartAsync("archive", secondId, second);
        var committed = await resumedParts.CommitPartsAsync("archive", [secondId, firstId], new StorageWriteOptions { IfAbsent = true, ContentType = "application/zip" });
        committed.Length.ShouldBe(11);
        committed.ContentType.ShouldBe("application/zip");
        await using var stream = await resumedParts.OpenObjectReadAsync("archive");
        using var reader = new StreamReader(stream);
        (await reader.ReadToEndAsync()).ShouldBe("secondfirst");
        (await Should.ThrowAsync<StorageOperationException>(() => resumedParts.CommitPartsAsync("archive", [firstId], new StorageWriteOptions { IfAbsent = true }))).IsConflict.ShouldBeTrue();
    }

    [Fact]
    public async Task MetadataPaginationAndDeletion_PreserveContainerBoundary()
    {
        using var storage = CreateStorage();
        var objects = storage.RequireObjectStorage();
        await objects.CreatePrivateContainerAsync(new Dictionary<string, string> { ["owner"] = "one" });
        await objects.SetContainerMetadataAsync(new Dictionary<string, string> { ["owner"] = "two" });
        for (var index = 0; index < 3; index++)
        {
            using var content = Content("data");
            await objects.WriteObjectAsync($"files/{index}", content);
        }
        var page = await objects.ListObjectsAsync("files/", pageSize: 2);
        page.Items.Count.ShouldBe(2);
        page.ContinuationToken.ShouldNotBeNullOrEmpty();
        var next = await objects.ListObjectsAsync("files/", page.ContinuationToken, 2);
        next.Items.Count.ShouldBe(1);
        next.Items[0].Path.ShouldBe("files/2");
        var info = next.Items[0];
        await objects.SetObjectMetadataAsync(info.Path, new Dictionary<string, string> { ["hash"] = "value" }, info.ETag);
        (await objects.GetObjectInfoAsync(info.Path)).Metadata["hash"].ShouldBe("value");
        (await Should.ThrowAsync<StorageOperationException>(() => objects.SetObjectMetadataAsync(info.Path, new Dictionary<string, string>(), info.ETag))).IsConflict.ShouldBeTrue();
        (await objects.DeleteObjectIfExistsAsync(info.Path, includeSnapshots: true)).ShouldBeTrue();
        (await objects.ObjectExistsAsync(info.Path)).ShouldBeFalse();
        (await Should.ThrowAsync<StorageOperationException>(() => objects.GetObjectInfoAsync(info.Path))).IsNotFound.ShouldBeTrue();
        (await objects.GetContainerInfoAsync()).Metadata["owner"].ShouldBe("two");
        (await objects.DeleteContainerIfExistsAsync()).ShouldBeTrue();
        (await objects.DeleteContainerIfExistsAsync()).ShouldBeFalse();
    }

    [Fact]
    public async Task VfsEmptyWrites_CreateAndTruncateStoredFiles()
    {
        using var storage = CreateStorage();
        await storage.RequireObjectStorage().CreatePrivateContainerAsync();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddVirtualFileSystem(storage, options => options.EnableCache = false);
        await using var provider = services.BuildServiceProvider();
        var vfs = provider.GetRequiredService<IVirtualFileSystem>();
        var empty = await vfs.GetFileAsync(new VfsPath("/empty.txt"));
        await empty.WriteAllBytesAsync([]);
        (await storage.RequireObjectStorage().GetObjectInfoAsync("empty.txt")).Length.ShouldBe(0);
        await using (var emptyRead = await storage.RequireObjectStorage().OpenObjectReadAsync("empty.txt"))
        {
            (await emptyRead.ReadAsync(new byte[1])).ShouldBe(0);
        }
        var populated = await vfs.GetFileAsync(new VfsPath("/populated.txt"));
        await populated.WriteAllTextAsync("previous");
        await populated.WriteAllBytesAsync([], new ManagedCode.Storage.VirtualFileSystem.Options.WriteOptions { Overwrite = true });
        (await storage.RequireObjectStorage().GetObjectInfoAsync("populated.txt")).Length.ShouldBe(0);
    }

    private AzureStorage CreateStorage(string? name = null) => new(new AzureStorageOptions
    {
        ConnectionString = _container.GetConnectionString(),
        Container = name ?? $"objects-{Guid.NewGuid():N}",
        CreateContainerIfNotExists = false
    });

    private static MemoryStream Content(string text) => new(Encoding.UTF8.GetBytes(text));
}
