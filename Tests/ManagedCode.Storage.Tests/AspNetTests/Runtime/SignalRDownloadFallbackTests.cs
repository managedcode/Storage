using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using ManagedCode.Storage.Client.SignalR;
using ManagedCode.Storage.Core;
using ManagedCode.Storage.Core.Models;
using ManagedCode.Storage.Server.Hubs;
using ManagedCode.Storage.Server.Models;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Shouldly;
using Xunit;

namespace ManagedCode.Storage.Tests.AspNetTests.Runtime;

public sealed class SignalRDownloadFallbackTests(StorageHttpRuntimeHost host) : IClassFixture<StorageHttpRuntimeHost>
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task DownloadThroughAlias_CompletesFromBytesWhenFinalEventUsesTheBackingName(bool reportAliasProgress)
    {
        var token = TestContext.Current.CancellationToken;
        var prefix = reportAliasProgress ? AliasedDownloadHub.InitialProgressPrefix : "document-";
        var alias = $"{prefix}{Guid.NewGuid():N}.bin";
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
        downloaded.TransferId.ShouldBe(reportAliasProgress ? alias : string.Empty);
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
    internal const string InitialProgressPrefix = "progress-document-";

    public override async IAsyncEnumerable<byte[]> DownloadStreamAsync(string blobName, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (blobName.StartsWith(InitialProgressPrefix, StringComparison.Ordinal))
        {
            // The client receives progress for the alias before streaming resolves the backing name.
            await Clients.Caller.SendAsync(StorageHubEvents.TransferProgress, new TransferStatus
            {
                TransferId = blobName,
                Operation = "download",
                ResourceName = blobName
            }, cancellationToken);
        }

        await foreach (var chunk in base.DownloadStreamAsync(BackingPrefix + blobName, cancellationToken))
        {
            yield return chunk;
        }
    }
}
