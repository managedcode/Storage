using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using ManagedCode.Storage.Azure.Extensions;
using ManagedCode.Storage.Core;
using ManagedCode.Storage.Core.Primitives;
using ManagedCode.Storage.Tests.Common;
using ManagedCode.Storage.VirtualFileSystem.Core;
using ManagedCode.Storage.VirtualFileSystem.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Testcontainers.Azurite;
using Xunit;

namespace ManagedCode.Storage.Tests.VirtualFileSystem;

public sealed class VfsImmutableMetadataTests : IAsyncLifetime
{
    private readonly AzuriteContainer _container = new AzuriteBuilder(ContainerImages.Azurite)
        .WithCommand("--skipApiVersionCheck").Build();

    public Task InitializeAsync() => _container.StartAsync();
    public Task DisposeAsync() => _container.DisposeAsync().AsTask();

    [Theory]
    [InlineData(false, "material.txt")]
    [InlineData(true, "material.txt")]
    [InlineData(false, "матеріал.txt")]
    [InlineData(true, "матеріал.txt")]
    public async Task ImmutableWrite_PreservesMetadataThroughFreshAndPreviouslyEmptyHandles(bool cache, string name)
    {
        await using var provider = CreateProvider(cache);
        await using var scope = provider.CreateAsyncScope();
        var vfs = scope.ServiceProvider.GetRequiredService<IVirtualFileSystem>();
        var path = new VfsPath("/learning/material/content");
        var previous = await vfs.GetFileAsync(path);
        (await previous.GetMetadataAsync()).ShouldBeEmpty();
        (await vfs.FileExistsAsync(path)).ShouldBeFalse();
        var bytes = Encoding.UTF8.GetBytes("exact immutable author bytes");
        var metadata = new Dictionary<string, string>
        {
            ["fileName"] = name,
            ["ownerUserId"] = "exact-owner",
            ["contentType"] = "text/plain"
        };
        var options = new StorageWriteOptions { ContentType = "text/plain", Metadata = metadata };
        var written = await vfs.WriteBytesIfAbsentOrSameAsync(path, bytes, options);
        written.ReusedExisting.ShouldBeFalse();
        AssertMetadata(await previous.GetMetadataAsync(), metadata);
        (await vfs.FileExistsAsync(path)).ShouldBeTrue();
        var fresh = await vfs.GetFileAsync(path);
        await fresh.RefreshAsync();
        fresh.Size.ShouldBe(bytes.LongLength);
        AssertMetadata(await fresh.GetMetadataAsync(), metadata);
        await using var stream = await fresh.OpenReadAsync();
        using var content = new MemoryStream();
        await stream.CopyToAsync(content);
        content.ToArray().ShouldBe(bytes);
        var replay = await vfs.WriteBytesIfAbsentOrSameAsync(path, bytes, options);
        replay.ReusedExisting.ShouldBeTrue();
        replay.Info.ETag.ShouldBe(written.Info.ETag);
        AssertMetadata(await fresh.GetMetadataAsync(), metadata);
    }

    [Fact]
    public async Task MetadataRead_PreservesLegacyCustomFieldsAndFiltersOnlyReservedVfsFields()
    {
        await using var provider = CreateProvider(false);
        await using var scope = provider.CreateAsyncScope();
        var storage = scope.ServiceProvider.GetRequiredService<IStorage>();
        var vfs = scope.ServiceProvider.GetRequiredService<IVirtualFileSystem>();
        var metadata = new Dictionary<string, string>
        {
            ["ownerUserId"] = "exact-owner",
            ["x-vfs-reviewer"] = "legacy-custom",
            ["x-vfs-review-vfs-created"] = "user-field",
            ["x-vfs-vfs-version"] = "1",
            ["x-vfs-vfs-created"] = DateTimeOffset.UtcNow.ToString("O"),
            ["x-vfs-vfs-modified"] = DateTimeOffset.UtcNow.ToString("O"),
            ["x-vfs-vfs-attributes"] = "0",
            ["x-vfs-vfs-internal"] = "private-vfs-state"
        };
        await storage.RequireObjectStorage().CreatePrivateContainerAsync();
        using var bytes = new MemoryStream(Encoding.UTF8.GetBytes("legacy bytes"));
        await storage.RequireObjectStorage().WriteObjectAsync("legacy/content", bytes,
            new StorageWriteOptions { Metadata = metadata });
        var file = await vfs.GetFileAsync("/legacy/content");
        AssertMetadata(await file.GetMetadataAsync(), new Dictionary<string, string>
        {
            ["ownerUserId"] = "exact-owner",
            ["reviewer"] = "legacy-custom",
            ["review-vfs-created"] = "user-field"
        });
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Refresh_AfterMultipartCommit_ExposesCommittedBytes(bool cache)
    {
        await using var provider = CreateProvider(cache);
        await using var scope = provider.CreateAsyncScope();
        var vfs = scope.ServiceProvider.GetRequiredService<IVirtualFileSystem>();
        var path = new VfsPath("/learning/commands/output.zip");
        var file = await vfs.GetFileAsync(path);
        (await file.ExistsAsync()).ShouldBeFalse();
        await file.RefreshAsync();
        var bytes = Encoding.UTF8.GetBytes("committed archive bytes");
        var multipart = vfs.Storage.RequireMultipartStorage();
        await multipart.CreatePrivateContainerAsync();
        var partId = Convert.ToBase64String(Encoding.UTF8.GetBytes("archive-part-0001"));
        using var part = new MemoryStream(bytes);
        await multipart.StagePartAsync(path.ToBlobKey(), partId, part);
        await multipart.CommitPartsAsync(path.ToBlobKey(), [partId]);
        await file.RefreshAsync();
        file.Size.ShouldBe(bytes.LongLength);
        (await file.ExistsAsync()).ShouldBeTrue();
        (await file.ReadAllBytesAsync()).ShouldBe(bytes);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Refresh_AfterProviderDeletion_ReportsMissingFile(bool cache)
    {
        await using var provider = CreateProvider(cache);
        await using var scope = provider.CreateAsyncScope();
        var vfs = scope.ServiceProvider.GetRequiredService<IVirtualFileSystem>();
        var path = new VfsPath("/learning/commands/deleted.zip");
        var file = await vfs.GetFileAsync(path);
        await file.WriteAllBytesAsync(Encoding.UTF8.GetBytes("previous archive bytes"));
        (await file.ExistsAsync()).ShouldBeTrue();
        (await vfs.Storage.RequireObjectStorage().DeleteObjectIfExistsAsync(path.ToBlobKey())).ShouldBeTrue();
        await file.RefreshAsync();
        file.Size.ShouldBe(0);
        (await file.ExistsAsync()).ShouldBeFalse();
    }

    private ServiceProvider CreateProvider(bool cache)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        var containerName = $"vfs-metadata-{Guid.NewGuid():N}";
        services.AddAzureStorageAsDefault(options =>
        {
            options.ConnectionString = _container.GetConnectionString();
            options.Container = containerName;
            options.CreateContainerIfNotExists = true;
        });
        services.AddVirtualFileSystem(options =>
        {
            options.DefaultContainer = containerName;
            options.EnableCache = cache;
        });
        return services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
    }

    private static void AssertMetadata(IReadOnlyDictionary<string, string> actual, Dictionary<string, string> expected)
    {
        actual.Count.ShouldBe(expected.Count);
        foreach (var item in expected)
        {
            actual[item.Key].ShouldBe(item.Value);
        }
    }
}
