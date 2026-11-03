using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using ManagedCode.Storage.Azure;
using ManagedCode.Storage.Client.SignalR;
using ManagedCode.Storage.Client.SignalR.Models;
using ManagedCode.Storage.Core.Models;
using ManagedCode.Storage.Tests.AspNetTests.Abstracts;
using ManagedCode.Storage.Tests.Common;
using ManagedCode.Storage.Tests.Constants;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Xunit;

namespace ManagedCode.Storage.Tests.AspNetTests.Azure;

public sealed class SignalRRuntimeContractsTests(StorageTestApplication app) : BaseSignalRStorageTests(app, ApiEndpoints.Azure)
{
    [Fact]
    public async Task ProgressStatusAndStreaming_DeliverTheStoredBytesAndResourceIdentity()
    {
        var token = TestContext.Current.CancellationToken;
        var name = Guid.NewGuid().ToString("N") + ".bin";
        var bytes = RandomNumberGenerator.GetBytes(131079);
        await using var client = CreateClient();
        await client.ConnectAsync(token);
        await client.ConnectAsync(token);
        var started = new TaskCompletionSource<StorageTransferStatus>(TaskCreationOptions.RunContinuationsAsynchronously);
        var uploadProgress = new StatusRecorder(s => { if (s.BytesTransferred > 0 && !s.IsCompleted) started.TrySetResult(s); });
        await using var input = new GatedReadStream(bytes);
        var descriptor = CreateDescriptor(name, "application/octet-stream", bytes.Length);
        var upload = client.UploadAsync(input, descriptor, uploadProgress, token);
        try
        {
            var active = await started.Task.WaitAsync(TimeSpan.FromSeconds(30), token);
            var liveStatus = await client.GetStatusAsync(active.TransferId, token);
            liveStatus.ShouldNotBeNull();
            liveStatus!.IsCompleted.ShouldBeFalse();
            liveStatus.BytesTransferred.ShouldBeGreaterThan(0);
            await using var stranger = CreateClient();
            await stranger.ConnectAsync(token);
            (await stranger.GetStatusAsync(active.TransferId, token)).ShouldBeNull();
            await stranger.CancelTransferAsync(active.TransferId, token);
        }
        finally { input.Resume(); }
        var status = await upload;
        status.IsCompleted.ShouldBeTrue();
        uploadProgress.Statuses.ShouldNotBeEmpty();
        uploadProgress.Statuses.All(s => s.TransferId == status.TransferId).ShouldBeTrue();
        var downloadProgress = new StatusRecorder();
        await using var destination = new MemoryStream();
        (await client.DownloadAsync(name, destination, downloadProgress, token)).IsCompleted.ShouldBeTrue();
        destination.ToArray().ShouldBe(bytes);
        downloadProgress.Statuses.ShouldNotBeEmpty();
        downloadProgress.Statuses.All(s => s.ResourceName == name).ShouldBeTrue();
        await using var chunks = new MemoryStream();
        await foreach (var chunk in client.DownloadStreamAsync(name, token)) await chunks.WriteAsync(chunk, token);
        chunks.ToArray().ShouldBe(bytes);
        (await client.GetStatusAsync("missing-transfer", token)).ShouldBeNull();
        await client.CancelTransferAsync(status.TransferId, token);
        await using var scope = TestApplication.Services.CreateAsyncScope();
        (await scope.ServiceProvider.GetRequiredService<IAzureStorage>().DeleteAsync(name, token)).IsSuccess.ShouldBeTrue();
        await client.DisconnectAsync(token);
        client.IsConnected.ShouldBeFalse();
    }

    [Fact]
    public async Task MissingDownload_PropagatesTheRealHubFailure()
    {
        await using var client = CreateClient();
        var token = TestContext.Current.CancellationToken;
        await client.ConnectAsync(token);
        var name = "missing-" + Guid.NewGuid().ToString("N");
        var terminal = new TaskCompletionSource<StorageTransferStatus>(TaskCreationOptions.RunContinuationsAsynchronously);
        client.TransferFaulted += (_, status) => terminal.TrySetResult(status);
        await using var output = new MemoryStream();

        await Should.ThrowAsync<HubException>(() => client.DownloadAsync(name, output, new StatusRecorder(), token));

        output.Length.ShouldBe(0);
        var fault = await terminal.Task.WaitAsync(TimeSpan.FromSeconds(5), token);
        fault.ResourceName.ShouldBe(name);
        fault.Operation.ShouldBe("download");
        fault.BytesTransferred.ShouldBe(0);
        fault.IsCompleted.ShouldBeFalse();
        fault.Error.ShouldBe("Download failed");
        (await client.GetStatusAsync(fault.TransferId, token)).ShouldBeNull();
    }

    [Theory]
    [InlineData(2)]
    [InlineData(4)]
    public async Task UploadWithWrongLength_DoesNotCommit(int declared)
    {
        var token = TestContext.Current.CancellationToken;
        await using var client = CreateClient();
        await client.ConnectAsync(token);
        var name = $"wrong-size-{Guid.NewGuid():N}.bin";
        using var input = new MemoryStream(new byte[3]);

        await Should.ThrowAsync<HubException>(() => client.UploadAsync(
            input, CreateDescriptor(name, "application/octet-stream", declared), cancellationToken: token));

        await using var scope = TestApplication.Services.CreateAsyncScope();
        var storage = scope.ServiceProvider.GetRequiredService<IAzureStorage>();
        (await storage.ExistsAsync(name, token)).Value.ShouldBeFalse();
    }

    [Fact]
    public async Task ClientTransferId_CannotSelectTheStagingFilePath()
    {
        var token = TestContext.Current.CancellationToken;
        var id = Path.Combine(Environment.CurrentDirectory, $"signalr-{Guid.NewGuid():N}");
        var outside = id + ".upload";
        var name = $"isolated-{Guid.NewGuid():N}.bin";
        await using var scope = TestApplication.Services.CreateAsyncScope();
        var storage = scope.ServiceProvider.GetRequiredService<IAzureStorage>();
        try
        {
            await File.WriteAllTextAsync(outside, "original", token);
            await using var client = CreateClient();
            await client.ConnectAsync(token);
            var descriptor = CreateDescriptor(name, "application/octet-stream", 1);
            descriptor.TransferId = id;
            using var input = new MemoryStream(new byte[1]);

            (await client.UploadAsync(input, descriptor, cancellationToken: token)).IsCompleted.ShouldBeTrue();

            (await File.ReadAllTextAsync(outside, token)).ShouldBe("original");
        }
        finally
        {
            File.Delete(outside);
            await storage.DeleteAsync(name, token);
        }
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    public async Task InvalidTransferLimits_AreRejectedBeforePublishingAnUpload(int invalid)
    {
        await using var client = CreateClient(options => { if (invalid == 0) options.StreamBufferSize = 0; else options.UploadChannelCapacity = 0; });
        var token = TestContext.Current.CancellationToken;
        await client.ConnectAsync(token);
        await using var input = new MemoryStream(new byte[] { 1 });
        await Should.ThrowAsync<InvalidOperationException>(() => client.UploadAsync(input, CreateDescriptor("invalid.bin", "application/octet-stream", 1), cancellationToken: token));
    }

    private sealed class StatusRecorder(Action<StorageTransferStatus>? received = null) : IProgress<StorageTransferStatus>
    {
        public ConcurrentQueue<StorageTransferStatus> Statuses { get; } = new();
        public void Report(StorageTransferStatus value) { Statuses.Enqueue(value); received?.Invoke(value); }
    }

    private sealed class GatedReadStream(byte[] bytes) : MemoryStream(bytes)
    {
        private readonly TaskCompletionSource<bool> _resume = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private int _reads;
        public void Resume() => _resume.TrySetResult(true);
        public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            if (Interlocked.Increment(ref _reads) > 1) await _resume.Task.WaitAsync(cancellationToken);
            return await base.ReadAsync(buffer, cancellationToken);
        }
        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) =>
            ReadAsync(buffer.AsMemory(offset, count), cancellationToken).AsTask();
    }
}
