using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using ManagedCode.Communication;
using ManagedCode.Storage.Core.Models;
using ManagedCode.Storage.FileSystem;
using ManagedCode.Storage.FileSystem.Options;
using ManagedCode.Storage.Tests.VirtualFileSystem.Fixtures;
using ManagedCode.Storage.VirtualFileSystem.Core;
using ManagedCode.Storage.VirtualFileSystem.Options;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Shouldly;
using Xunit;
using VfsImplementation = ManagedCode.Storage.VirtualFileSystem.Implementations.VirtualFileSystem;

namespace ManagedCode.Storage.Tests.VirtualFileSystem;

public sealed class StrictFileExistenceTests
{
    [Fact]
    public async Task StorageFileExistsAsync_PropagatesProviderFailure()
    {
        using var storage = new FailedExistenceStorage();
        using var cache = new MemoryCache(new MemoryCacheOptions());
        await using var fileSystem = new VfsImplementation(
            storage,
            new TestMetadataManager(storage),
            Options.Create(new VfsOptions { EnableCache = true }),
            cache,
            NullLogger<VfsImplementation>.Instance);

        await Should.ThrowAsync<ProblemException>(async () =>
            await fileSystem.StorageFileExistsAsync(new VfsPath("/unavailable.txt")));
    }

    private sealed class FailedExistenceStorage()
        : FileSystemStorage(new FileSystemStorageOptions { BaseFolder = Path.GetTempPath() })
    {
        protected override Task<Result<bool>> ExistsInternalAsync(
            ExistOptions options, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Result<bool>.Fail(new IOException("Storage unavailable.")));
        }
    }
}
