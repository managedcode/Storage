using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ManagedCode.Storage.Core.Helpers;
using ManagedCode.Storage.Core.Models;
using ManagedCode.Storage.VirtualFileSystem.Core;
using ManagedCode.Storage.VirtualFileSystem.Options;
using ManagedCode.Storage.VirtualFileSystem.Exceptions;
using ManagedCode.Storage.Tests.VirtualFileSystem.Fixtures;
using ManagedCode.Storage.Tests.Common;
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
        await using var context = await CreateContextAsync();
        var vfs = context.FileSystem;

        var file = await vfs.GetFileAsync(new VfsPath("/docs/readme.txt"), TestContext.Current.CancellationToken);
        await file.WriteAllTextAsync("Hello Virtual FS!", cancellationToken: TestContext.Current.CancellationToken);

        var content = await file.ReadAllTextAsync(cancellationToken: TestContext.Current.CancellationToken);
        content.ShouldBe("Hello Virtual FS!");

        (await vfs.FileExistsAsync(new VfsPath("/docs/readme.txt"), TestContext.Current.CancellationToken)).ShouldBeTrue();
    }

    [Fact]
    public async Task FileExistsAsync_ShouldCacheResults()
    {
        await using var context = await CreateContextAsync();
        var vfs = context.FileSystem;
        var metadataManager = context.MetadataManager;

        var path = new VfsPath("/cache/sample.txt");
        var file = await vfs.GetFileAsync(path, TestContext.Current.CancellationToken);
        await file.WriteAllTextAsync("cached", cancellationToken: TestContext.Current.CancellationToken);

        metadataManager.ResetCounters();
        var firstCheck = await vfs.FileExistsAsync(path, TestContext.Current.CancellationToken);
        firstCheck.ShouldBeTrue();
        metadataManager.BlobInfoRequests.ShouldBe(0);

        metadataManager.ResetCounters();
        var secondCheck = await vfs.FileExistsAsync(path, TestContext.Current.CancellationToken);
        secondCheck.ShouldBeTrue();
        metadataManager.BlobInfoRequests.ShouldBe(0);
    }

    [Fact]
    public async Task StorageFileExistsAsync_BypassesCachedAbsence()
    {
        await using var context = await CreateContextAsync();
        var path = new VfsPath($"/fresh/{Guid.NewGuid():N}.txt");
        (await context.FileSystem.FileExistsAsync(path, TestContext.Current.CancellationToken)).ShouldBeFalse();

        var upload = await context.Storage.UploadAsync("created externally", new UploadOptions
        {
            FileName = path.ToBlobKey()
        }, TestContext.Current.CancellationToken);
        upload.ThrowIfProblem();

        (await context.FileSystem.StorageFileExistsAsync(path, TestContext.Current.CancellationToken)).ShouldBeTrue();
        (await context.FileSystem.FileExistsAsync(path, TestContext.Current.CancellationToken)).ShouldBeFalse();
    }

    [Fact]
    public async Task ListAsync_ShouldEnumerateAllEntries()
    {
        if (!Capabilities.SupportsListing)
        {
            return;
        }

        await using var context = await CreateContextAsync();
        var vfs = context.FileSystem;
        var metadataManager = context.MetadataManager;

        for (var i = 0; i < 5; i++)
        {
            var file = await vfs.GetFileAsync(new VfsPath($"/reports/file-{i}.txt"), TestContext.Current.CancellationToken);
            await file.WriteAllTextAsync($"report-{i}", cancellationToken: TestContext.Current.CancellationToken);
        }

        var sampleMetadata = await metadataManager.GetBlobInfoAsync("reports/file-0.txt", TestContext.Current.CancellationToken);
        sampleMetadata.ShouldNotBeNull();
        sampleMetadata!.FullName.ShouldBe("reports/file-0.txt");

        var entries = new List<IVfsNode>();
        await foreach (var entry in vfs.ListAsync(new VfsPath("/reports"), new ListOptions { PageSize = 2 }, TestContext.Current.CancellationToken))
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
        await using var context = await CreateContextAsync();
        var vfs = context.FileSystem;
        var metadataManager = context.MetadataManager;

        var path = new VfsPath("/temp/remove.me");
        var file = await vfs.GetFileAsync(path, TestContext.Current.CancellationToken);
        await file.WriteAllTextAsync("to delete", cancellationToken: TestContext.Current.CancellationToken);

        metadataManager.ResetCounters();
        await vfs.FileExistsAsync(path, TestContext.Current.CancellationToken);
        metadataManager.ResetCounters();

        var deleted = await file.DeleteAsync(TestContext.Current.CancellationToken);
        deleted.ShouldBeTrue();

        var existsAfterDelete = await vfs.FileExistsAsync(path, TestContext.Current.CancellationToken);
        existsAfterDelete.ShouldBeFalse();
        metadataManager.BlobInfoRequests.ShouldBe(0);

        metadataManager.ResetCounters();
        var secondCheck = await vfs.FileExistsAsync(path, TestContext.Current.CancellationToken);
        secondCheck.ShouldBeFalse();
        metadataManager.BlobInfoRequests.ShouldBe(0);
    }

    [Fact]
    public async Task FileExistsAsync_WhenFileIsMissing_ShouldNotReadMetadata()
    {
        await using var context = await CreateContextAsync();
        var path = new VfsPath($"/missing/{Guid.NewGuid():N}.txt");

        context.MetadataManager.ResetCounters();
        var exists = await context.FileSystem.FileExistsAsync(path, TestContext.Current.CancellationToken);

        exists.ShouldBeFalse();
        context.MetadataManager.BlobInfoRequests.ShouldBe(0);
    }

    [Fact]
    public async Task GetMetadataAsync_ShouldCacheCustomMetadata()
    {
        await using var context = await CreateContextAsync();
        var vfs = context.FileSystem;
        var metadataManager = context.MetadataManager;

        var file = await vfs.GetFileAsync(new VfsPath("/meta/info.txt"), TestContext.Current.CancellationToken);
        await file.WriteAllTextAsync("meta", cancellationToken: TestContext.Current.CancellationToken);

        await file.SetMetadataAsync(new Dictionary<string, string>
        {
            ["owner"] = "qa",
            ["region"] = "eu"
        }, cancellationToken: TestContext.Current.CancellationToken);

        metadataManager.ResetCounters();
        var metadata = await file.GetMetadataAsync(TestContext.Current.CancellationToken);
        metadata.ShouldContainKey("owner");
        metadataManager.CustomMetadataRequests.ShouldBe(1);

        metadataManager.ResetCounters();
        var secondLookup = await file.GetMetadataAsync(TestContext.Current.CancellationToken);
        secondLookup.ShouldContainKey("region");
        metadataManager.CustomMetadataRequests.ShouldBe(0);
    }

    [Theory]
    [MemberData(nameof(UnicodeVfsTestCases.FolderScenarios), MemberType = typeof(UnicodeVfsTestCases))]
    public async Task WriteAndReadFile_WithUnicodeDirectories_ShouldRoundtrip(
        string directoryName,
        string fileName,
        string content)
    {
        await using var context = await CreateContextAsync();
        var vfs = context.FileSystem;

        var path = new VfsPath($"/international/{directoryName}/{fileName}.txt");
        var file = await vfs.GetFileAsync(path, TestContext.Current.CancellationToken);

        await file.WriteAllTextAsync(content, cancellationToken: TestContext.Current.CancellationToken);

        (await file.ReadAllTextAsync(cancellationToken: TestContext.Current.CancellationToken)).ShouldBe(content);
        file.Path.GetFileName().ShouldBe($"{fileName}.txt");
        file.Path.GetFileNameWithoutExtension().ShouldBe(fileName);
        file.Path.GetExtension().ShouldBe(".txt");
        file.Path.GetParent().Value.ShouldBe($"/international/{directoryName}");
        file.Path.ToBlobKey().ShouldBe($"international/{directoryName}/{fileName}.txt");

        (await vfs.FileExistsAsync(path, TestContext.Current.CancellationToken)).ShouldBeTrue();

        if (Capabilities.SupportsListing)
        {
            var entries = new List<IVfsNode>();
            await foreach (var entry in vfs.ListAsync(new VfsPath($"/international/{directoryName}"), new ListOptions
            {
                IncludeFiles = true,
                IncludeDirectories = false,
                Recursive = false
            }, TestContext.Current.CancellationToken))
            {
                entries.Add(entry);
            }

            entries.ShouldContain(e => e.Path.Value == path.Value);
        }

        (await file.DeleteAsync(TestContext.Current.CancellationToken)).ShouldBeTrue();
        (await vfs.FileExistsAsync(path, TestContext.Current.CancellationToken)).ShouldBeFalse();
    }

    [Fact]
    public async Task DeleteDirectoryAsync_NonRecursive_ShouldPreserveNestedContent()
    {
        if (!Capabilities.SupportsDirectoryDelete)
        {
            return;
        }

        await using var context = await CreateContextAsync();
        var vfs = context.FileSystem;

        await (await vfs.GetFileAsync(new VfsPath("/nonrec/root.txt"), TestContext.Current.CancellationToken)).WriteAllTextAsync("root", cancellationToken: TestContext.Current.CancellationToken);
        await (await vfs.GetFileAsync(new VfsPath("/nonrec/sub/nested.txt"), TestContext.Current.CancellationToken)).WriteAllTextAsync("child", cancellationToken: TestContext.Current.CancellationToken);

        var result = await vfs.DeleteDirectoryAsync(new VfsPath("/nonrec"), recursive: false, cancellationToken: TestContext.Current.CancellationToken);
        result.FilesDeleted.ShouldBe(1);

        (await vfs.FileExistsAsync(new VfsPath("/nonrec/root.txt"), TestContext.Current.CancellationToken)).ShouldBeFalse();
        (await vfs.FileExistsAsync(new VfsPath("/nonrec/sub/nested.txt"), TestContext.Current.CancellationToken)).ShouldBeTrue();
    }

    [Fact]
    public async Task DeleteDirectoryAsync_Recursive_ShouldRemoveAllContent()
    {
        if (!Capabilities.SupportsDirectoryDelete)
        {
            return;
        }

        await using var context = await CreateContextAsync();
        var vfs = context.FileSystem;

        await (await vfs.GetFileAsync(new VfsPath("/recursive/root.txt"), TestContext.Current.CancellationToken)).WriteAllTextAsync("root", cancellationToken: TestContext.Current.CancellationToken);
        await (await vfs.GetFileAsync(new VfsPath("/recursive/sub/nested.txt"), TestContext.Current.CancellationToken)).WriteAllTextAsync("child", cancellationToken: TestContext.Current.CancellationToken);

        var result = await vfs.DeleteDirectoryAsync(new VfsPath("/recursive"), recursive: true, cancellationToken: TestContext.Current.CancellationToken);
        result.FilesDeleted.ShouldBe(2);

        (await vfs.FileExistsAsync(new VfsPath("/recursive/root.txt"), TestContext.Current.CancellationToken)).ShouldBeFalse();
        (await vfs.FileExistsAsync(new VfsPath("/recursive/sub/nested.txt"), TestContext.Current.CancellationToken)).ShouldBeFalse();
    }

    [Fact]
    public async Task MoveAsync_ShouldRelocateFile()
    {
        if (!Capabilities.SupportsMove)
        {
            return;
        }

        await using var context = await CreateContextAsync();
        var vfs = context.FileSystem;

        var sourcePath = new VfsPath("/docs/report.pdf");
        var destPath = new VfsPath("/archive/report.pdf");
        var file = await vfs.GetFileAsync(sourcePath, TestContext.Current.CancellationToken);
        await file.WriteAllBytesAsync(new byte[] { 1, 2, 3, 4 }, cancellationToken: TestContext.Current.CancellationToken);

        await vfs.MoveAsync(sourcePath, destPath, cancellationToken: TestContext.Current.CancellationToken);

        var moved = await vfs.GetFileAsync(destPath, TestContext.Current.CancellationToken);
        var bytes = await moved.ReadAllBytesAsync(TestContext.Current.CancellationToken);
        bytes.ShouldBe(new byte[] { 1, 2, 3, 4 });

        var original = await vfs.GetFileAsync(sourcePath, TestContext.Current.CancellationToken);
        await Should.ThrowAsync<VfsException>(() => original.ReadAllBytesAsync());
    }

    [Fact]
    public async Task CopyAsync_ShouldCopyDirectoryRecursively()
    {
        if (!Capabilities.SupportsDirectoryCopy)
        {
            return;
        }

        await using var context = await CreateContextAsync();
        var vfs = context.FileSystem;

        for (var i = 0; i < 3; i++)
        {
            var file = await vfs.GetFileAsync(new VfsPath($"/src/data-{i}.bin"), TestContext.Current.CancellationToken);
            await file.WriteAllBytesAsync(new byte[] { (byte)i }, cancellationToken: TestContext.Current.CancellationToken);
        }

        var nested = await vfs.GetFileAsync(new VfsPath("/src/nested/item.txt"), TestContext.Current.CancellationToken);
        await nested.WriteAllTextAsync("nested", cancellationToken: TestContext.Current.CancellationToken);

        await vfs.CopyAsync(new VfsPath("/src"), new VfsPath("/dest"), new CopyOptions { Recursive = true, Overwrite = true }, cancellationToken: TestContext.Current.CancellationToken);

        for (var i = 0; i < 3; i++)
        {
            var copied = await vfs.GetFileAsync(new VfsPath($"/dest/data-{i}.bin"), TestContext.Current.CancellationToken);
            var bytes = await copied.ReadAllBytesAsync(TestContext.Current.CancellationToken);
            bytes.ShouldBe(new byte[] { (byte)i });
        }

        var copiedNested = await vfs.GetFileAsync(new VfsPath("/dest/nested/item.txt"), TestContext.Current.CancellationToken);
        (await copiedNested.ReadAllTextAsync(cancellationToken: TestContext.Current.CancellationToken)).ShouldBe("nested");
    }

    [Fact]
    public async Task ReadRangeAsync_ShouldReturnSlice()
    {
        await using var context = await CreateContextAsync();
        var vfs = context.FileSystem;

        var file = await vfs.GetFileAsync(new VfsPath("/ranges/sample.bin"), TestContext.Current.CancellationToken);
        await file.WriteAllBytesAsync(Enumerable.Range(0, 100).Select(i => (byte)i).ToArray(), cancellationToken: TestContext.Current.CancellationToken);

        var slice = await file.ReadRangeAsync(0, 5, TestContext.Current.CancellationToken);
        slice.ShouldBe(new byte[] { 0, 1, 2, 3, 4 });
    }

    [Fact]
    public async Task ListAsync_WithDirectoryFilter_ShouldExcludeDirectoriesWhenRequested()
    {
        if (!Capabilities.SupportsListing)
        {
            return;
        }

        await using var context = await CreateContextAsync();
        var vfs = context.FileSystem;

        await (await vfs.GetFileAsync(new VfsPath("/filter/a.txt"), TestContext.Current.CancellationToken)).WriteAllTextAsync("A", cancellationToken: TestContext.Current.CancellationToken);
        await (await vfs.GetFileAsync(new VfsPath("/filter/b.log"), TestContext.Current.CancellationToken)).WriteAllTextAsync("B", cancellationToken: TestContext.Current.CancellationToken);

        var entries = new List<IVfsNode>();
        await foreach (var entry in vfs.ListAsync(new VfsPath("/filter"), new ListOptions
        {
            IncludeDirectories = false,
            IncludeFiles = true,
            Recursive = false
        }, TestContext.Current.CancellationToken))
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
        if (!Capabilities.SupportsDirectoryStats)
        {
            return;
        }

        await using var context = await CreateContextAsync();
        var vfs = context.FileSystem;

        await (await vfs.GetFileAsync(new VfsPath("/stats/one.txt"), TestContext.Current.CancellationToken)).WriteAllTextAsync("one", cancellationToken: TestContext.Current.CancellationToken);
        await (await vfs.GetFileAsync(new VfsPath("/stats/two.bin"), TestContext.Current.CancellationToken)).WriteAllBytesAsync(new byte[] { 1, 2, 3, 4 }, cancellationToken: TestContext.Current.CancellationToken);

        var directory = await vfs.GetDirectoryAsync(new VfsPath("/stats"), TestContext.Current.CancellationToken);
        var stats = await directory.GetStatsAsync(cancellationToken: TestContext.Current.CancellationToken);

        stats.FileCount.ShouldBeGreaterThanOrEqualTo(2);
        stats.FilesByExtension.ShouldContainKey(".txt");
        stats.FilesByExtension.ShouldContainKey(".bin");
    }

    [Theory]
    [Trait("Category", "LargeFile")]
    [InlineData(1)]
    [InlineData(3)]
    [InlineData(5)]
    public async Task LargeFile_ShouldRoundTripViaStreams(int gigabytes)
    {
        var sizeBytes = LargeFileTestHelper.ResolveSizeBytes(gigabytes);

        await using var context = await CreateContextAsync();
        var vfs = context.FileSystem;

        await using var sourceFile = await LargeFileTestHelper.CreateRandomFileAsync(sizeBytes, ".bin", cancellationToken: TestContext.Current.CancellationToken);
        var expectedCrc = LargeFileTestHelper.CalculateFileCrc(sourceFile.FilePath);

        var path = new VfsPath($"/large/{Guid.NewGuid():N}.bin");
        var file = await vfs.GetFileAsync(path, TestContext.Current.CancellationToken);

        await using (var writeStream = await file.OpenWriteAsync(cancellationToken: CancellationToken.None))
        await using (var readSource = File.OpenRead(sourceFile.FilePath))
        {
            await readSource.CopyToAsync(writeStream, cancellationToken: CancellationToken.None);
        }

        await using (var readBack = await file.OpenReadAsync(cancellationToken: CancellationToken.None))
        {
            var actualCrc = Crc32Helper.CalculateStreamCrc(readBack);
            actualCrc.ShouldBe(expectedCrc);
        }

        (await file.DeleteAsync(TestContext.Current.CancellationToken)).ShouldBeTrue();
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
