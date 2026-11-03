using System.IO;
using System.Threading;
using System.Threading.Tasks;
using ManagedCode.Storage.VirtualFileSystem.Exceptions;
using ManagedCode.Storage.VirtualFileSystem.Options;
using Shouldly;
using Xunit;

namespace ManagedCode.Storage.Tests.VirtualFileSystem;

public sealed class VfsMoveSafetyTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task DirectoryMove_WithDestinationConflict_PreservesBothVersions(bool cachedMiss)
    {
        await using var session = await LocalVfsSession.CreateAsync();
        var token = TestContext.Current.CancellationToken;
        var source = await session.FileSystem.GetFileAsync("/source/file.txt", token);
        var destination = await session.FileSystem.GetFileAsync("/destination/file.txt", token);
        await source.WriteAllTextAsync("source bytes", cancellationToken: token);
        if (cachedMiss)
        {
            (await destination.ExistsAsync(token)).ShouldBeFalse();
            var destinationDirectory = Directory.CreateDirectory(Path.Combine(session.RootDirectory, "destination"));
            await File.WriteAllTextAsync(Path.Combine(destinationDirectory.FullName, "file.txt"), "destination bytes", token);
        }
        else
        {
            await destination.WriteAllTextAsync("destination bytes", cancellationToken: token);
        }

        await Should.ThrowAsync<VfsAlreadyExistsException>(() =>
            session.FileSystem.MoveAsync("/source", "/destination", cancellationToken: token));

        (await source.ReadAllTextAsync(cancellationToken: token)).ShouldBe("source bytes");
        (await File.ReadAllTextAsync(Path.Combine(session.RootDirectory, "destination", "file.txt"), token)).ShouldBe("destination bytes");
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Move_WhenLegalHoldPreventsSourceDeletion_ReportsFailure(bool directory)
    {
        await using var session = await LocalVfsSession.CreateAsync();
        var token = TestContext.Current.CancellationToken;
        var source = await session.FileSystem.GetFileAsync("/source/file.txt", token);
        await source.WriteAllTextAsync("held bytes", cancellationToken: token);
        (await session.Storage.SetLegalHoldAsync(true, "source/file.txt", token)).IsSuccess.ShouldBeTrue();

        try
        {
            await Should.ThrowAsync<VfsOperationException>(() => session.FileSystem.MoveAsync(
                directory ? "/source" : "/source/file.txt", directory ? "/destination" : "/destination/file.txt",
                cancellationToken: token));

            (await source.ReadAllTextAsync(cancellationToken: token)).ShouldBe("held bytes");
            var copy = await session.FileSystem.GetFileAsync("/destination/file.txt", token);
            (await copy.ReadAllTextAsync(cancellationToken: token)).ShouldBe("held bytes");
        }
        finally
        {
            (await session.Storage.SetLegalHoldAsync(false, "source/file.txt", CancellationToken.None)).IsSuccess.ShouldBeTrue();
        }
    }

    [Theory]
    [InlineData("/tree/file.txt", "/tree/./file.txt")]
    [InlineData("/tree", "/tree/")]
    [InlineData("/tree", "/tree/nested")]
    [InlineData("/tree/nested", "/tree")]
    public async Task Move_WithOverlappingPaths_LeavesOriginalBytes(string source, string destination)
    {
        await using var session = await LocalVfsSession.CreateAsync();
        var token = TestContext.Current.CancellationToken;
        var originalPath = source.EndsWith(".txt", System.StringComparison.Ordinal) ? source : source + "/file.txt";
        var original = await session.FileSystem.GetFileAsync(originalPath, token);
        await original.WriteAllTextAsync("original bytes", cancellationToken: token);

        await Should.ThrowAsync<VfsOperationException>(() =>
            session.FileSystem.MoveAsync(source, destination, new MoveOptions { Overwrite = true }, token));

        (await original.ReadAllTextAsync(cancellationToken: token)).ShouldBe("original bytes");
        Directory.GetFiles(session.RootDirectory, "*", SearchOption.AllDirectories).Length.ShouldBe(1);
    }

    [Fact]
    public async Task DirectoryMove_WithSimilarSiblingPrefix_RelocatesFiles()
    {
        await using var session = await LocalVfsSession.CreateAsync();
        var token = TestContext.Current.CancellationToken;
        var original = await session.FileSystem.GetFileAsync("/tree/nested/file.txt", token);
        await original.WriteAllTextAsync("original bytes", cancellationToken: token);

        await session.FileSystem.MoveAsync("/tree", "/tree-backup", cancellationToken: token);

        (await original.ExistsAsync(token)).ShouldBeFalse();
        var moved = await session.FileSystem.GetFileAsync("/tree-backup/nested/file.txt", token);
        (await moved.ReadAllTextAsync(cancellationToken: token)).ShouldBe("original bytes");
    }
}
