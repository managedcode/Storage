using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ManagedCode.Storage.Core.Models;
using ManagedCode.Storage.VirtualFileSystem.Core;
using ManagedCode.Storage.VirtualFileSystem.Options;
using ManagedCode.Storage.VirtualFileSystem.Exceptions;
using ManagedCode.Storage.Tests.VirtualFileSystem.Fixtures;
using Shouldly;
using Xunit;

namespace ManagedCode.Storage.Tests.VirtualFileSystem;

public abstract class VirtualFileSystemTests<TFixture> : IClassFixture<TFixture>
    where TFixture : class, IVirtualFileSystemFixture
{
    private readonly TFixture _fixture;

    protected VirtualFileSystemTests(TFixture fixture)
    {
        _fixture = fixture;
    }

    private Task<VirtualFileSystemTestContext> CreateContextAsync() => _fixture.CreateContextAsync();
    private VirtualFileSystemCapabilities Capabilities => _fixture.Capabilities;

    [Fact]
    public async Task WriteAndReadFile_ShouldRoundtrip()
    {
        if (!Capabilities.Enabled)
        {
            return;
        }

        await using var context = await CreateContextAsync();
        var vfs = context.FileSystem;

        var file = await vfs.GetFileAsync(new VfsPath("/docs/readme.txt"));
        await file.WriteAllTextAsync("Hello Virtual FS!");

        var content = await file.ReadAllTextAsync();
        content.ShouldBe("Hello Virtual FS!");

        (await vfs.FileExistsAsync(new VfsPath("/docs/readme.txt"))).ShouldBeTrue();
    }

    [Fact]
    public async Task FileExistsAsync_ShouldCacheResults()
    {
        if (!Capabilities.Enabled)
        {
            return;
        }

        await using var context = await CreateContextAsync();
        var vfs = context.FileSystem;
        var metadataManager = context.MetadataManager;

        var path = new VfsPath("/cache/sample.txt");
        var file = await vfs.GetFileAsync(path);
        await file.WriteAllTextAsync("cached");

        metadataManager.ResetCounters();
        var firstCheck = await vfs.FileExistsAsync(path);
        firstCheck.ShouldBeTrue();
        metadataManager.BlobInfoRequests.ShouldBe(1);

        metadataManager.ResetCounters();
        var secondCheck = await vfs.FileExistsAsync(path);
        secondCheck.ShouldBeTrue();
        metadataManager.BlobInfoRequests.ShouldBe(0);
    }

    [Fact]
    public async Task ListAsync_ShouldEnumerateAllEntries()
    {
        if (!Capabilities.Enabled || !Capabilities.SupportsListing)
        {
            return;
        }

        await using var context = await CreateContextAsync();
        var vfs = context.FileSystem;
        var metadataManager = context.MetadataManager;

        for (var i = 0; i < 5; i++)
        {
            var file = await vfs.GetFileAsync(new VfsPath($"/reports/file-{i}.txt"));
            await file.WriteAllTextAsync($"report-{i}");
        }

        var sampleMetadata = await metadataManager.GetBlobInfoAsync("reports/file-0.txt");
        sampleMetadata.ShouldNotBeNull();
        sampleMetadata!.FullName.ShouldBe("reports/file-0.txt");

        var entries = new List<IVfsNode>();
        await foreach (var entry in vfs.ListAsync(new VfsPath("/reports"), new ListOptions { PageSize = 2 }))
        {
            entries.Add(entry);
        }

        var fileEntries = entries.OfType<IVirtualFile>().ToList();
        fileEntries.Count.ShouldBe(5);
        var names = fileEntries.Select(f => f.Path.GetFileName()).OrderBy(n => n).ToList();
        names.ShouldBe(new[]
        {
            "file-0.txt", "file-1.txt", "file-2.txt", "file-3.txt", "file-4.txt"
        });
    }

    [Fact]
    public async Task DeleteFile_ShouldRemoveFromUnderlyingStorage()
    {
        if (!Capabilities.Enabled)
        {
            return;
        }

        await using var context = await CreateContextAsync();
        var vfs = context.FileSystem;
        var metadataManager = context.MetadataManager;

        var path = new VfsPath("/temp/remove.me");
        var file = await vfs.GetFileAsync(path);
        await file.WriteAllTextAsync("to delete");

        metadataManager.ResetCounters();
        await vfs.FileExistsAsync(path);
        metadataManager.ResetCounters();

        var deleted = await file.DeleteAsync();
        deleted.ShouldBeTrue();

        var existsAfterDelete = await vfs.FileExistsAsync(path);
        existsAfterDelete.ShouldBeFalse();
        metadataManager.BlobInfoRequests.ShouldBe(1);

        metadataManager.ResetCounters();
        var secondCheck = await vfs.FileExistsAsync(path);
        secondCheck.ShouldBeFalse();
        metadataManager.BlobInfoRequests.ShouldBe(0);
    }

    [Fact]
    public async Task GetMetadataAsync_ShouldCacheCustomMetadata()
    {
        if (!Capabilities.Enabled)
        {
            return;
        }

        await using var context = await CreateContextAsync();
        var vfs = context.FileSystem;
        var metadataManager = context.MetadataManager;

        var file = await vfs.GetFileAsync(new VfsPath("/meta/info.txt"));
        await file.WriteAllTextAsync("meta");

        await file.SetMetadataAsync(new Dictionary<string, string>
        {
            ["owner"] = "qa",
            ["region"] = "eu"
        });

        metadataManager.ResetCounters();
        var metadata = await file.GetMetadataAsync();
        metadata.ShouldContainKey("owner");
        metadataManager.CustomMetadataRequests.ShouldBe(1);

        metadataManager.ResetCounters();
        var secondLookup = await file.GetMetadataAsync();
        secondLookup.ShouldContainKey("region");
        metadataManager.CustomMetadataRequests.ShouldBe(0);
    }

    [Fact]
    public async Task DeleteDirectoryAsync_NonRecursive_ShouldPreserveNestedContent()
    {
        if (!Capabilities.Enabled || !Capabilities.SupportsDirectoryDelete)
        {
            return;
        }

        await using var context = await CreateContextAsync();
        var vfs = context.FileSystem;

        await (await vfs.GetFileAsync(new VfsPath("/nonrec/root.txt"))).WriteAllTextAsync("root");
        await (await vfs.GetFileAsync(new VfsPath("/nonrec/sub/nested.txt"))).WriteAllTextAsync("child");

        var result = await vfs.DeleteDirectoryAsync(new VfsPath("/nonrec"), recursive: false);
        result.FilesDeleted.ShouldBe(1);

        (await vfs.FileExistsAsync(new VfsPath("/nonrec/root.txt"))).ShouldBeFalse();
        (await vfs.FileExistsAsync(new VfsPath("/nonrec/sub/nested.txt"))).ShouldBeTrue();
    }

    [Fact]
    public async Task DeleteDirectoryAsync_Recursive_ShouldRemoveAllContent()
    {
        if (!Capabilities.Enabled || !Capabilities.SupportsDirectoryDelete)
        {
            return;
        }

        await using var context = await CreateContextAsync();
        var vfs = context.FileSystem;

        await (await vfs.GetFileAsync(new VfsPath("/recursive/root.txt"))).WriteAllTextAsync("root");
        await (await vfs.GetFileAsync(new VfsPath("/recursive/sub/nested.txt"))).WriteAllTextAsync("child");

        var result = await vfs.DeleteDirectoryAsync(new VfsPath("/recursive"), recursive: true);
        result.FilesDeleted.ShouldBe(2);

        (await vfs.FileExistsAsync(new VfsPath("/recursive/root.txt"))).ShouldBeFalse();
        (await vfs.FileExistsAsync(new VfsPath("/recursive/sub/nested.txt"))).ShouldBeFalse();
    }

    [Fact]
    public async Task MoveAsync_ShouldRelocateFile()
    {
        if (!Capabilities.Enabled || !Capabilities.SupportsMove)
        {
            return;
        }

        await using var context = await CreateContextAsync();
        var vfs = context.FileSystem;

        var sourcePath = new VfsPath("/docs/report.pdf");
        var destPath = new VfsPath("/archive/report.pdf");
        var file = await vfs.GetFileAsync(sourcePath);
        await file.WriteAllBytesAsync(new byte[] { 1, 2, 3, 4 });

        await vfs.MoveAsync(sourcePath, destPath);

        var moved = await vfs.GetFileAsync(destPath);
        var bytes = await moved.ReadAllBytesAsync();
        bytes.ShouldBe(new byte[] { 1, 2, 3, 4 });

        var original = await vfs.GetFileAsync(sourcePath);
        await Should.ThrowAsync<VfsException>(() => original.ReadAllBytesAsync());
    }

    [Fact]
    public async Task CopyAsync_ShouldCopyDirectoryRecursively()
    {
        if (!Capabilities.Enabled || !Capabilities.SupportsDirectoryCopy)
        {
            return;
        }

        await using var context = await CreateContextAsync();
        var vfs = context.FileSystem;

        for (var i = 0; i < 3; i++)
        {
            var file = await vfs.GetFileAsync(new VfsPath($"/src/data-{i}.bin"));
            await file.WriteAllBytesAsync(new byte[] { (byte)i });
        }

        var nested = await vfs.GetFileAsync(new VfsPath("/src/nested/item.txt"));
        await nested.WriteAllTextAsync("nested");

        await vfs.CopyAsync(new VfsPath("/src"), new VfsPath("/dest"), new CopyOptions { Recursive = true, Overwrite = true });

        for (var i = 0; i < 3; i++)
        {
            var copied = await vfs.GetFileAsync(new VfsPath($"/dest/data-{i}.bin"));
            var bytes = await copied.ReadAllBytesAsync();
            bytes.ShouldBe(new byte[] { (byte)i });
        }

        var copiedNested = await vfs.GetFileAsync(new VfsPath("/dest/nested/item.txt"));
        (await copiedNested.ReadAllTextAsync()).ShouldBe("nested");
    }

    [Fact]
    public async Task ReadRangeAsync_ShouldReturnSlice()
    {
        if (!Capabilities.Enabled)
        {
            return;
        }

        await using var context = await CreateContextAsync();
        var vfs = context.FileSystem;

        var file = await vfs.GetFileAsync(new VfsPath("/ranges/sample.bin"));
        await file.WriteAllBytesAsync(Enumerable.Range(0, 100).Select(i => (byte)i).ToArray());

        var slice = await file.ReadRangeAsync(0, 5);
        slice.ShouldBe(new byte[] { 0, 1, 2, 3, 4 });
    }

    [Fact]
    public async Task ListAsync_WithDirectoryFilter_ShouldExcludeDirectoriesWhenRequested()
    {
        if (!Capabilities.Enabled || !Capabilities.SupportsListing)
        {
            return;
        }

        await using var context = await CreateContextAsync();
        var vfs = context.FileSystem;

        await (await vfs.GetFileAsync(new VfsPath("/filter/a.txt"))).WriteAllTextAsync("A");
        await (await vfs.GetFileAsync(new VfsPath("/filter/b.log"))).WriteAllTextAsync("B");

        var entries = new List<IVfsNode>();
        await foreach (var entry in vfs.ListAsync(new VfsPath("/filter"), new ListOptions
        {
            IncludeDirectories = false,
            IncludeFiles = true,
            Recursive = false
        }))
        {
            entries.Add(entry);
        }

        entries.Count.ShouldBe(2);
        entries.ShouldAllBe(e => e.Type == VfsEntryType.File);

        var paths = entries.OfType<IVirtualFile>().Select(e => e.Path.Value).OrderBy(v => v).ToList();
        paths.ShouldBe(new[] { "/filter/a.txt", "/filter/b.log" });
    }

    [Fact]
    public async Task DirectoryStats_ShouldAggregateInformation()
    {
        if (!Capabilities.Enabled || !Capabilities.SupportsDirectoryStats)
        {
            return;
        }

        await using var context = await CreateContextAsync();
        var vfs = context.FileSystem;

        await (await vfs.GetFileAsync(new VfsPath("/stats/one.txt"))).WriteAllTextAsync("one");
        await (await vfs.GetFileAsync(new VfsPath("/stats/two.bin"))).WriteAllBytesAsync(new byte[] { 1, 2, 3, 4 });

        var directory = await vfs.GetDirectoryAsync(new VfsPath("/stats"));
        var stats = await directory.GetStatsAsync();

        stats.FileCount.ShouldBeGreaterThanOrEqualTo(2);
        stats.FilesByExtension.ShouldContainKey(".txt");
        stats.FilesByExtension.ShouldContainKey(".bin");
    }
}

[Collection(VirtualFileSystemCollection.Name)]
public sealed class FileSystemVirtualFileSystemTests : VirtualFileSystemTests<FileSystemVirtualFileSystemFixture>
{
    public FileSystemVirtualFileSystemTests(FileSystemVirtualFileSystemFixture fixture) : base(fixture)
    {
    }
}

[Collection(VirtualFileSystemCollection.Name)]
public sealed class AzureVirtualFileSystemTests : VirtualFileSystemTests<AzureVirtualFileSystemFixture>
{
    public AzureVirtualFileSystemTests(AzureVirtualFileSystemFixture fixture) : base(fixture)
    {
    }
}

[Collection(VirtualFileSystemCollection.Name)]
public sealed class AwsVirtualFileSystemTests : VirtualFileSystemTests<AwsVirtualFileSystemFixture>
{
    public AwsVirtualFileSystemTests(AwsVirtualFileSystemFixture fixture) : base(fixture)
    {
    }
}

[Collection(VirtualFileSystemCollection.Name)]
public sealed class GcsVirtualFileSystemTests : VirtualFileSystemTests<GcsVirtualFileSystemFixture>
{
    public GcsVirtualFileSystemTests(GcsVirtualFileSystemFixture fixture) : base(fixture)
    {
    }
}

[Collection(VirtualFileSystemCollection.Name)]
public sealed class SftpVirtualFileSystemTests : VirtualFileSystemTests<SftpVirtualFileSystemFixture>
{
    public SftpVirtualFileSystemTests(SftpVirtualFileSystemFixture fixture) : base(fixture)
    {
    }
}
