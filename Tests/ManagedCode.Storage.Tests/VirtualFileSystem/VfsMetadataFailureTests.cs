using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using ManagedCode.Communication;
using ManagedCode.Storage.Core.Models;
using ManagedCode.Storage.FileSystem;
using ManagedCode.Storage.FileSystem.Options;
using ManagedCode.Storage.VirtualFileSystem.Core;
using ManagedCode.Storage.VirtualFileSystem.Exceptions;
using ManagedCode.Storage.VirtualFileSystem.Extensions;
using ManagedCode.Storage.VirtualFileSystem.Metadata;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Xunit;

namespace ManagedCode.Storage.Tests.VirtualFileSystem;

public sealed class VfsMetadataFailureTests
{
    [Theory]
    [InlineData("bytes")]
    [InlineData("refresh")]
    [InlineData("metadata")]
    [InlineData("custom")]
    public async Task CancelledMetadataRead_DoesNotBecomeMissingOrPoisonTheNextRead(string operation)
    {
        await using var session = await Session.CreateAsync();
        var file = await session.Files.GetFileAsync(new VfsPath("/event.json"), TestContext.Current.CancellationToken);
        await file.WriteAllTextAsync("retained-event", cancellationToken: TestContext.Current.CancellationToken);
        using var cancelled = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        cancelled.Cancel();
        await Should.ThrowAsync<OperationCanceledException>(async () =>
        {
            switch (operation)
            {
                case "bytes": await file.ReadAllBytesAsync(cancelled.Token); break;
                case "refresh": await file.RefreshAsync(cancelled.Token); break;
                case "metadata": await session.Metadata.GetVfsMetadataAsync("event.json", cancelled.Token); break;
                case "custom": await session.Metadata.GetCustomMetadataAsync("event.json", cancelled.Token); break;
                default: throw new ArgumentOutOfRangeException(nameof(operation));
            }
        });
        (await file.ReadAllTextAsync(cancellationToken: TestContext.Current.CancellationToken)).ShouldBe("retained-event");
        (await session.Files.StorageFileExistsAsync(new VfsPath("/event.json"), TestContext.Current.CancellationToken)).ShouldBeTrue();
    }

    [Theory]
    [InlineData("info")]
    [InlineData("metadata")]
    [InlineData("custom")]
    public async Task FailedProviderMetadataRead_PreservesFailureInsteadOfClaimingAbsence(string operation)
    {
        await using var session = await Session.CreateAsync();
        // The real file provider rejects an empty key. No storage double or
        // mocked failure is involved: VFS must preserve that provider failure.
        await Should.ThrowAsync<ProblemException>(async () =>
        {
            switch (operation)
            {
                case "info": await session.Metadata.GetBlobInfoAsync(string.Empty, TestContext.Current.CancellationToken); break;
                case "metadata": await session.Metadata.GetVfsMetadataAsync(string.Empty, TestContext.Current.CancellationToken); break;
                case "custom": await session.Metadata.GetCustomMetadataAsync(string.Empty, TestContext.Current.CancellationToken); break;
                default: throw new ArgumentOutOfRangeException(nameof(operation));
            }
        });
    }

    [Fact]
    public async Task ActualMissingFile_StillReportsNotFoundAndCanBeCreatedAndRead()
    {
        await using var session = await Session.CreateAsync();
        var metadata = await session.Storage.GetBlobMetadataAsync("event.json", TestContext.Current.CancellationToken);
        metadata.IsFailed.ShouldBeTrue();
        metadata.Problem.StatusCode.ShouldBe(404);
        (await session.Metadata.GetBlobInfoAsync("event.json", TestContext.Current.CancellationToken)).ShouldBeNull();
        var file = await session.Files.GetFileAsync(new VfsPath("/event.json"), TestContext.Current.CancellationToken);
        await Should.ThrowAsync<VfsNotFoundException>(() => file.ReadAllBytesAsync(TestContext.Current.CancellationToken));
        await file.WriteAllTextAsync("now-present", cancellationToken: TestContext.Current.CancellationToken);
        await file.RefreshAsync(TestContext.Current.CancellationToken);
        (await file.ReadAllTextAsync(cancellationToken: TestContext.Current.CancellationToken)).ShouldBe("now-present");
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task ExistingHandle_ObservesWritesAndDeletesAfterLoadingMissingMetadata(bool cache)
    {
        await using var session = await LocalVfsSession.CreateAsync(cache);
        var token = TestContext.Current.CancellationToken;
        var observer = await session.FileSystem.GetFileAsync("/observed.bin", token);
        var writer = await session.FileSystem.GetFileAsync("/observed.bin", token);
        await Should.ThrowAsync<VfsNotFoundException>(() => observer.OpenReadAsync(cancellationToken: token));

        await writer.WriteAllBytesAsync([2, 5, 9], cancellationToken: token);
        (await observer.ReadAllBytesAsync(token)).ShouldBe(new byte[] { 2, 5, 9 });
        observer.Size.ShouldBe(3);

        (await writer.DeleteAsync(token)).ShouldBeTrue();
        await Should.ThrowAsync<VfsNotFoundException>(() => observer.OpenReadAsync(cancellationToken: token));
    }

    [Theory]
    [InlineData(true, 1)]
    [InlineData(false, 2)]
    public async Task Refresh_UsesOneProviderMetadataReadAndHonorsSharedCaching(bool cache, int expectedReads)
    {
        await using var session = await LocalVfsSession.CreateAsync(cache, createStorage: options => new MetadataCountingStorage(options));
        var token = TestContext.Current.CancellationToken;
        var file = await session.FileSystem.GetFileAsync("/counted.bin", token);
        await file.WriteAllBytesAsync([2, 5, 9], cancellationToken: token);
        var storage = (MetadataCountingStorage)session.Storage;
        var before = storage.MetadataReads;

        await file.RefreshAsync(token);

        file.Size.ShouldBe(3);
        (storage.MetadataReads - before).ShouldBe(1);
        var secondHandle = await session.FileSystem.GetFileAsync("/counted.bin", token);
        await using var opened = await secondHandle.OpenReadAsync(cancellationToken: token);
        opened.ReadByte().ShouldBe(2);
        secondHandle.CreatedOn.ShouldBe(file.CreatedOn);
        (storage.MetadataReads - before).ShouldBe(expectedReads);
    }

    private sealed class MetadataCountingStorage(FileSystemStorageOptions options) : FileSystemStorage(options)
    {
        public int MetadataReads { get; private set; }

        protected override Task<Result<BlobMetadata>> GetBlobMetadataInternalAsync(MetadataOptions options,
            CancellationToken cancellationToken = default)
        {
            MetadataReads++;
            return base.GetBlobMetadataInternalAsync(options, cancellationToken);
        }
    }

    private sealed class Session(string root, ServiceProvider services, FileSystemStorage storage) : IAsyncDisposable
    {
        public FileSystemStorage Storage { get; } = storage;
        public IVirtualFileSystem Files { get; } = services.GetRequiredService<IVirtualFileSystem>();
        public IMetadataManager Metadata { get; } = services.GetRequiredService<IMetadataManager>();

        public static async Task<Session> CreateAsync()
        {
            var directory = new DirectoryInfo(AppContext.BaseDirectory);
            while (!File.Exists(Path.Combine(directory.FullName, "ManagedCode.Storage.slnx")))
                directory = directory.Parent ?? throw new DirectoryNotFoundException("Storage repository root not found.");
            var root = Path.Combine(directory.FullName, "artifacts", "vfs-metadata-tests", Guid.NewGuid().ToString("N"));
            var storage = new FileSystemStorage(new FileSystemStorageOptions { BaseFolder = root, CreateContainerIfNotExists = true });
            (await storage.CreateContainerAsync(TestContext.Current.CancellationToken)).IsSuccess.ShouldBeTrue();
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddVirtualFileSystem(storage, options => options.EnableCache = false);
            return new Session(root, services.BuildServiceProvider(), storage);
        }

        public async ValueTask DisposeAsync()
        {
            await services.DisposeAsync();
            if (Directory.Exists(root)) Directory.Delete(root, true);
        }
    }
}
