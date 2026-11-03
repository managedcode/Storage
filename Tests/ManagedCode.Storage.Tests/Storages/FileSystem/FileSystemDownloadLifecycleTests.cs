using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using ManagedCode.Communication;
using ManagedCode.Storage.Core.Models;
using ManagedCode.Storage.FileSystem;
using ManagedCode.Storage.FileSystem.Options;
using Shouldly;
using Xunit;

namespace ManagedCode.Storage.Tests.Storages.FileSystem;

public sealed class FileSystemDownloadLifecycleTests
{
    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(true, true)]
    public async Task DownloadAsync_ReleasesUnusedTempFileAndPreservesExplicitDestination(bool exists, bool explicitPath)
    {
        var token = TestContext.Current.CancellationToken;
        var root = Directory.CreateTempSubdirectory();
        var source = Path.Combine(root.FullName, "source.txt");
        var destination = Path.Combine(root.FullName, "destination.txt");
        try
        {
            using var storage = new DownloadTrackingStorage(new FileSystemStorageOptions { BaseFolder = root.FullName });
            if (exists)
                await File.WriteAllTextAsync(source, "source content", token);
            if (explicitPath)
                await File.WriteAllTextAsync(destination, "existing content", token);

            var result = await storage.DownloadAsync(
                new DownloadOptions { FileName = "source.txt", LocalPath = explicitPath ? destination : null }, token);
            using var candidate = storage.Candidate.ShouldNotBeNull();
            result.IsSuccess.ShouldBe(exists);
            if (result.IsSuccess)
            {
                using var downloaded = result.Value;
                downloaded.FilePath.ShouldBe(explicitPath ? destination : source);
                (await downloaded.ReadAllTextAsync(token)).ShouldBe("source content");
            }

            File.Exists(candidate.FilePath).ShouldBe(explicitPath);
            if (explicitPath && !exists)
                (await File.ReadAllTextAsync(destination, token)).ShouldBe("existing content");
            File.Exists(source).ShouldBe(exists);
        }
        finally
        {
            root.Delete(recursive: true);
        }
    }

    private sealed class DownloadTrackingStorage(FileSystemStorageOptions options) : FileSystemStorage(options)
    {
        public LocalFile? Candidate { get; private set; }

        protected override Task<Result<LocalFile>> DownloadInternalAsync(
            LocalFile localFile, DownloadOptions options, CancellationToken cancellationToken = default)
        {
            Candidate = localFile;
            return base.DownloadInternalAsync(localFile, options, cancellationToken);
        }
    }
}
