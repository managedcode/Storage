using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using ManagedCode.Storage.Client.SignalR;
using ManagedCode.Storage.Core;
using ManagedCode.Storage.Core.Models;
using ManagedCode.Storage.Server.Hubs;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.Extensions.Logging;
using Shouldly;
using Xunit;

namespace ManagedCode.Storage.Tests.AspNetTests.Runtime;

public sealed class SignalRDownloadFallbackTests(StorageHttpRuntimeHost host) : IClassFixture<StorageHttpRuntimeHost>
{
    [Fact]
    public async Task DownloadThroughAlias_CompletesFromBytesWhenEventsUseTheBackingName()
    {
        var token = TestContext.Current.CancellationToken;
        var alias = $"document-{Guid.NewGuid():N}.bin";
        var backingName = AliasedDownloadHub.BackingPrefix + alias;
        byte[] bytes = [1, 5, 9, 14];
        (await host.Storage.UploadAsync(bytes, new UploadOptions(backingName), token)).IsSuccess.ShouldBeTrue();
        await using var client = new StorageSignalRClient(new StorageSignalRClientOptions
        {
            HubUrl = new Uri(host.Client.BaseAddress!, AliasedDownloadHub.Route),
            TransportType = HttpTransportType.LongPolling
        });
        var completedName = new TaskCompletionSource<string?>(TaskCreationOptions.RunContinuationsAsynchronously);
        client.TransferCompleted += (_, status) => completedName.TrySetResult(status.ResourceName);
        await client.ConnectAsync(token);
        using var output = new MemoryStream();

        var downloaded = await client.DownloadAsync(alias, output, cancellationToken: token);

        (await completedName.Task.WaitAsync(TimeSpan.FromSeconds(5), token)).ShouldBe(backingName);
        downloaded.IsCompleted.ShouldBeTrue();
        downloaded.ResourceName.ShouldBe(alias);
        downloaded.BytesTransferred.ShouldBe(bytes.LongLength);
        downloaded.TotalBytes.ShouldBe(bytes.LongLength);
        output.ToArray().ShouldBe(bytes);
    }
}

public sealed class AliasedDownloadHub(IStorage storage, StorageHubOptions options, ILogger<AliasedDownloadHub> logger)
    : StorageHubBase<IStorage>(storage, options, logger)
{
    internal const string Route = "/hubs/aliased-storage";
    internal const string BackingPrefix = "backing/";

    public override IAsyncEnumerable<byte[]> DownloadStreamAsync(string blobName, CancellationToken cancellationToken = default) =>
        base.DownloadStreamAsync(BackingPrefix + blobName, cancellationToken);
}
