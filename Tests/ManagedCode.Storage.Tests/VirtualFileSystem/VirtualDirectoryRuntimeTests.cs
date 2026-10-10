using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ManagedCode.Storage.VirtualFileSystem.Core;
using ManagedCode.Storage.VirtualFileSystem.Options;
using Shouldly;
using Xunit;

namespace ManagedCode.Storage.Tests.VirtualFileSystem;

public sealed class VirtualDirectoryRuntimeTests
{
    [Theory]
    [InlineData(true, 1)]
    [InlineData(false, 0)]
    public async Task ListingAndStatistics_DescribeActualNestedFiles(bool cache, int pageSize)
    {
        await using var session = await LocalVfsSession.CreateAsync(cache, pageSize: pageSize);
        var vfs = session.FileSystem;
        var token = TestContext.Current.CancellationToken;
        foreach (var path in new[] { "/docs/first.txt", "/docs/child/second.txt", "/docs/child/deeper/last.bin" })
        {
            var file = await vfs.GetFileAsync(new VfsPath(path), token);
            await file.WriteAllTextAsync("content", cancellationToken: token);
        }
        var directory = await vfs.GetDirectoryAsync(new VfsPath("/docs"), token);
        directory.Name.ShouldBe("docs");
        directory.Type.ShouldBe(VfsEntryType.Directory);
        (await directory.ExistsAsync(token)).ShouldBeTrue();
        (await directory.GetParentAsync(token)).Name.ShouldBe("/");
        var files = new List<string>();
        await foreach (var file in directory.GetFilesAsync(new SearchPattern("*.txt"), true, pageSize, token))
            files.Add(file.Path.Value);
        files.Order().ShouldBe(new[] { "/docs/child/second.txt", "/docs/first.txt" });
        var directories = new List<string>();
        await foreach (var child in directory.GetDirectoriesAsync(recursive: true, pageSize: pageSize, cancellationToken: token))
            directories.Add(child.Path.Value);
        directories.Order().ShouldBe(new[] { "/docs/child", "/docs/child/deeper" });
        var stats = await directory.GetStatsAsync(cancellationToken: token);
        stats.FileCount.ShouldBe(3);
        stats.DirectoryCount.ShouldBe(2);
        stats.TotalSize.ShouldBe(21);
        stats.FilesByExtension[".txt"].ShouldBe(2);
        stats.LargestFile.ShouldNotBeNull();
        stats.LargestFile.Size.ShouldBe(7);
        stats.OldestModified.ShouldNotBeNull();
        var shallow = await directory.GetStatsAsync(false, token);
        shallow.FileCount.ShouldBe(1);
        shallow.TotalSize.ShouldBe(7);
    }

    [Theory]
    [InlineData(DirectoryStrategy.Virtual)]
    [InlineData(DirectoryStrategy.DotKeepFile)]
    public async Task DirectoryCreation_UsesConfiguredStrategyAndRejectsEmptyNames(DirectoryStrategy strategy)
    {
        await using var session = await LocalVfsSession.CreateAsync(strategy: strategy);
        var token = TestContext.Current.CancellationToken;
        var root = await session.FileSystem.GetDirectoryAsync(new VfsPath("/"), token);
        var directory = await root.CreateDirectoryAsync("empty", token);
        (await directory.ExistsAsync(token)).ShouldBe(strategy == DirectoryStrategy.DotKeepFile);
        if (strategy == DirectoryStrategy.DotKeepFile)
        {
            await directory.RefreshAsync(token);
            var marker = await session.FileSystem.GetFileAsync(new VfsPath("/empty/.keep"), token);
            (await marker.ReadAllBytesAsync(token)).ShouldBeEmpty();
        }
        await Should.ThrowAsync<ArgumentException>(async () => await root.CreateDirectoryAsync(" ", token));
        await Should.ThrowAsync<ArgumentException>(async () => await root.CreateFileAsync("", cancellationToken: token));
        var empty = await root.CreateFileAsync("empty.txt", cancellationToken: token);
        (await empty.ReadAllBytesAsync(token)).ShouldBeEmpty();
        await Should.ThrowAsync<ManagedCode.Storage.VirtualFileSystem.Exceptions.VfsAlreadyExistsException>(
            async () => await root.CreateFileAsync("empty.txt", cancellationToken: token));
        await root.CreateFileAsync("empty.txt", new CreateFileOptions { Overwrite = true }, token);
        (await root.DeleteAsync(true, token)).ShouldNotBeNull();
    }
}
