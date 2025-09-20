using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using ManagedCode.Storage.Core.Models;
using ManagedCode.Storage.FileSystem;
using ManagedCode.Storage.FileSystem.Options;
using ManagedCode.Storage.VirtualFileSystem.Core;
using VfsImplementation = ManagedCode.Storage.VirtualFileSystem.Implementations.VirtualFileSystem;
using ManagedCode.Storage.VirtualFileSystem.Metadata;
using ManagedCode.Storage.VirtualFileSystem.Options;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace ManagedCode.Storage.Tests.VirtualFileSystem;

public class VirtualFileSystemTests : IAsyncLifetime
{
    private FileSystemStorage _storage = null!;
    private string _basePath = null!;
    private InMemoryMetadataManager _metadataManager = null!;

    public Task InitializeAsync()
    {
        _basePath = Path.Combine(Path.GetTempPath(), "managedcode-vfs-tests", Guid.NewGuid().ToString());
        Directory.CreateDirectory(_basePath);

        var options = new FileSystemStorageOptions
        {
            BaseFolder = _basePath,
            CreateContainerIfNotExists = true
        };

        _storage = new FileSystemStorage(options);
        return Task.CompletedTask;
    }

    public Task DisposeAsync()
    {
        _storage.Dispose();
        if (Directory.Exists(_basePath))
        {
            Directory.Delete(_basePath, recursive: true);
        }

        return Task.CompletedTask;
    }

    private VfsImplementation CreateVirtualFileSystem()
    {
        var metadataManager = new InMemoryMetadataManager(_storage);
        _metadataManager = metadataManager;
        var cache = new MemoryCache(new MemoryCacheOptions());
        var options = Options.Create(new VfsOptions
        {
            DefaultContainer = string.Empty,
            DirectoryStrategy = DirectoryStrategy.Virtual,
            EnableCache = true
        });

        return new VfsImplementation(
            _storage,
            metadataManager,
            options,
            cache,
            NullLogger<VfsImplementation>.Instance);
    }

    [Fact]
    public async Task WriteAndReadFile_ShouldRoundtrip()
    {
        await using var vfs = CreateVirtualFileSystem();

        var file = await vfs.GetFileAsync(new VfsPath("/docs/readme.txt"));
        await file.WriteAllTextAsync("Hello Virtual FS!");

        var content = await file.ReadAllTextAsync();
        content.Should().Be("Hello Virtual FS!");

        var physicalPath = Path.Combine(_basePath, "docs", "readme.txt");
        File.Exists(physicalPath).Should().BeTrue();
    }

    [Fact]
    public async Task FileExistsAsync_ShouldCacheResults()
    {
        await using var vfs = CreateVirtualFileSystem();

        var path = new VfsPath("/cache/sample.txt");
        var file = await vfs.GetFileAsync(path);
        await file.WriteAllTextAsync("cached");

        _metadataManager.ResetCounters();
        var firstCheck = await vfs.FileExistsAsync(path);
        firstCheck.Should().BeTrue();
        _metadataManager.BlobInfoRequests.Should().Be(1);

        _metadataManager.ResetCounters();
        var secondCheck = await vfs.FileExistsAsync(path);
        secondCheck.Should().BeTrue();
        _metadataManager.BlobInfoRequests.Should().Be(0);
    }

    [Fact]
    public async Task ListAsync_ShouldEnumerateAllEntries()
    {
        await using var vfs = CreateVirtualFileSystem();

        for (var i = 0; i < 5; i++)
        {
            var file = await vfs.GetFileAsync(new VfsPath($"/reports/file-{i}.txt"));
            await file.WriteAllTextAsync($"report-{i}");
        }

        var sampleMetadata = await _metadataManager.GetBlobInfoAsync("reports/file-0.txt");
        sampleMetadata.Should().NotBeNull();
        sampleMetadata!.FullName.Should().Be("reports/file-0.txt");

        var entries = new List<IVfsEntry>();
        await foreach (var entry in vfs.ListAsync(new VfsPath("/reports"), new ListOptions { PageSize = 2 }))
        {
            entries.Add(entry);
        }

        var fileEntries = entries.OfType<IVirtualFile>().ToList();
        fileEntries.Should().HaveCount(5);
        var names = fileEntries.Select(f => f.Path.GetFileName()).OrderBy(n => n).ToList();
        names.Should().Contain(new[]
        {
            "file-0.txt", "file-1.txt", "file-2.txt", "file-3.txt", "file-4.txt"
        });
    }

    [Fact]
    public async Task DeleteFile_ShouldRemoveFromUnderlyingStorage()
    {
        await using var vfs = CreateVirtualFileSystem();

        var path = new VfsPath("/temp/remove.me");
        var file = await vfs.GetFileAsync(path);
        await file.WriteAllTextAsync("to delete");

        _metadataManager.ResetCounters();
        await vfs.FileExistsAsync(path);
        _metadataManager.ResetCounters();

        var deleted = await file.DeleteAsync();
        deleted.Should().BeTrue();

        var physicalPath = Path.Combine(_basePath, "temp", "remove.me");
        File.Exists(physicalPath).Should().BeFalse();

        var existsAfterDelete = await vfs.FileExistsAsync(path);
        existsAfterDelete.Should().BeFalse();
        _metadataManager.BlobInfoRequests.Should().Be(1);

        _metadataManager.ResetCounters();
        var secondCheck = await vfs.FileExistsAsync(path);
        secondCheck.Should().BeFalse();
        _metadataManager.BlobInfoRequests.Should().Be(0);
    }

    [Fact]
    public async Task GetMetadataAsync_ShouldCacheCustomMetadata()
    {
        await using var vfs = CreateVirtualFileSystem();

        var file = await vfs.GetFileAsync(new VfsPath("/meta/info.txt"));
        await file.WriteAllTextAsync("meta");

        await file.SetMetadataAsync(new Dictionary<string, string>
        {
            ["owner"] = "qa",
            ["region"] = "eu"
        });

        _metadataManager.ResetCounters();
        var metadata = await file.GetMetadataAsync();
        metadata.Should().ContainKey("owner");
        _metadataManager.CustomMetadataRequests.Should().Be(1);

        _metadataManager.ResetCounters();
        var secondLookup = await file.GetMetadataAsync();
        secondLookup.Should().ContainKey("region");
        _metadataManager.CustomMetadataRequests.Should().Be(0);
    }

    [Fact]
    public async Task DeleteDirectoryAsync_NonRecursive_ShouldPreserveNestedContent()
    {
        await using var vfs = CreateVirtualFileSystem();

        await (await vfs.GetFileAsync(new VfsPath("/nonrec/root.txt"))).WriteAllTextAsync("root");
        await (await vfs.GetFileAsync(new VfsPath("/nonrec/sub/nested.txt"))).WriteAllTextAsync("child");

        var result = await vfs.DeleteDirectoryAsync(new VfsPath("/nonrec"), recursive: false);
        result.FilesDeleted.Should().Be(1);

        File.Exists(Path.Combine(_basePath, "nonrec", "root.txt")).Should().BeFalse();
        File.Exists(Path.Combine(_basePath, "nonrec", "sub", "nested.txt")).Should().BeTrue();
    }

    [Fact]
    public async Task DeleteDirectoryAsync_Recursive_ShouldRemoveAllContent()
    {
        await using var vfs = CreateVirtualFileSystem();

        await (await vfs.GetFileAsync(new VfsPath("/recursive/root.txt"))).WriteAllTextAsync("root");
        await (await vfs.GetFileAsync(new VfsPath("/recursive/sub/nested.txt"))).WriteAllTextAsync("child");

        var result = await vfs.DeleteDirectoryAsync(new VfsPath("/recursive"), recursive: true);
        result.FilesDeleted.Should().Be(2);

        File.Exists(Path.Combine(_basePath, "recursive", "root.txt")).Should().BeFalse();
        File.Exists(Path.Combine(_basePath, "recursive", "sub", "nested.txt")).Should().BeFalse();
    }

    [Fact]
    public async Task MoveAsync_ShouldRelocateFile()
    {
        await using var vfs = CreateVirtualFileSystem();

        var sourcePath = new VfsPath("/docs/report.pdf");
        var destPath = new VfsPath("/archive/report.pdf");
        var file = await vfs.GetFileAsync(sourcePath);
        await file.WriteAllBytesAsync(new byte[] { 1, 2, 3, 4 });

        await vfs.MoveAsync(sourcePath, destPath);

        File.Exists(Path.Combine(_basePath, "docs", "report.pdf")).Should().BeFalse();
        File.Exists(Path.Combine(_basePath, "archive", "report.pdf")).Should().BeTrue();

        var moved = await vfs.GetFileAsync(destPath);
        var bytes = await moved.ReadAllBytesAsync();
        bytes.Should().Equal(1, 2, 3, 4);
    }

    [Fact]
    public async Task CopyAsync_ShouldCopyDirectoryRecursively()
    {
        await using var vfs = CreateVirtualFileSystem();

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
            File.Exists(Path.Combine(_basePath, "dest", $"data-{i}.bin")).Should().BeTrue();
        }

        File.Exists(Path.Combine(_basePath, "dest", "nested", "item.txt")).Should().BeTrue();
    }

    [Fact]
    public async Task ReadRangeAsync_ShouldReturnSlice()
    {
        await using var vfs = CreateVirtualFileSystem();

        var file = await vfs.GetFileAsync(new VfsPath("/ranges/sample.bin"));
        await file.WriteAllBytesAsync(Enumerable.Range(0, 100).Select(i => (byte)i).ToArray());

        var slice = await file.ReadRangeAsync(0, 5);
        slice.Should().Equal(0, 1, 2, 3, 4);
    }

    [Fact]
    public async Task ListAsync_WithDirectoryFilter_ShouldExcludeDirectoriesWhenRequested()
    {
        await using var vfs = CreateVirtualFileSystem();

        await (await vfs.GetFileAsync(new VfsPath("/filter/a.txt"))).WriteAllTextAsync("A");
        await (await vfs.GetFileAsync(new VfsPath("/filter/b.log"))).WriteAllTextAsync("B");

        var entries = new List<IVfsEntry>();
        await foreach (var entry in vfs.ListAsync(new VfsPath("/filter"), new ListOptions
        {
            IncludeDirectories = false,
            IncludeFiles = true,
            Recursive = false
        }))
        {
            entries.Add(entry);
        }

        entries.Should().HaveCount(2);
        entries.Should().OnlyContain(e => e.Type == VfsEntryType.File);

        var paths = entries.OfType<IVirtualFile>().Select(e => e.Path.Value).OrderBy(v => v).ToList();
        paths.Should().Equal("/filter/a.txt", "/filter/b.log");
    }

    [Fact]
    public async Task DirectoryStats_ShouldAggregateInformation()
    {
        await using var vfs = CreateVirtualFileSystem();

        await (await vfs.GetFileAsync(new VfsPath("/stats/one.txt"))).WriteAllTextAsync("one");
        await (await vfs.GetFileAsync(new VfsPath("/stats/two.bin"))).WriteAllBytesAsync(new byte[] { 1, 2, 3, 4 });

        var directory = await vfs.GetDirectoryAsync(new VfsPath("/stats"));
        var stats = await directory.GetStatsAsync();

        stats.FileCount.Should().Be(2);
        stats.FilesByExtension.Should().ContainKey(".txt");
        stats.FilesByExtension.Should().ContainKey(".bin");
    }

    private sealed class InMemoryMetadataManager : IMetadataManager
    {
        private readonly FileSystemStorage _storage;
        private readonly ConcurrentDictionary<string, VfsMetadata> _metadata = new();
        private readonly ConcurrentDictionary<string, IReadOnlyDictionary<string, string>> _custom = new();

        public int BlobInfoRequests { get; private set; }
        public int CustomMetadataRequests { get; private set; }

        public void ResetCounters()
        {
            BlobInfoRequests = 0;
            CustomMetadataRequests = 0;
        }

        public InMemoryMetadataManager(FileSystemStorage storage)
        {
            _storage = storage;
        }

        public Task SetVfsMetadataAsync(string blobName, VfsMetadata metadata, IDictionary<string, string>? customMetadata = null, string? expectedETag = null, CancellationToken cancellationToken = default)
        {
            _metadata[blobName] = metadata;
            _custom[blobName] = customMetadata is null
                ? new Dictionary<string, string>()
                : new Dictionary<string, string>(customMetadata);
            return Task.CompletedTask;
        }

        public Task<VfsMetadata?> GetVfsMetadataAsync(string blobName, CancellationToken cancellationToken = default)
        {
            _metadata.TryGetValue(blobName, out var metadata);
            return Task.FromResult(metadata);
        }

        public Task<IReadOnlyDictionary<string, string>> GetCustomMetadataAsync(string blobName, CancellationToken cancellationToken = default)
        {
            CustomMetadataRequests++;
            if (_custom.TryGetValue(blobName, out var metadata))
            {
                return Task.FromResult(metadata);
            }

            return Task.FromResult<IReadOnlyDictionary<string, string>>(new Dictionary<string, string>());
        }

        public async Task<BlobMetadata?> GetBlobInfoAsync(string blobName, CancellationToken cancellationToken = default)
        {
            BlobInfoRequests++;
            var result = await _storage.GetBlobMetadataAsync(blobName, cancellationToken);
            return result.IsSuccess ? result.Value : null;
        }
    }
}
