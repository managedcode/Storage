using System;
using System.Threading;
using System.Threading.Tasks;
using DotNet.Testcontainers.Containers;
using ManagedCode.Storage.Tests.Common;
using Shouldly;
using Xunit;
using Microsoft.Extensions.DependencyInjection;

namespace ManagedCode.Storage.Tests.Storages.Abstracts;

public abstract class UnavailableStorageContracts<T> : BaseContainer<T> where T : IContainer
{
    [Fact]
    public async Task ServiceOutage_ReturnsFailuresForStorageOperations()
    {
        var token = TestContext.Current.CancellationToken;
        (await Storage.UploadAsync("seed", cancellationToken: token)).IsSuccess.ShouldBeTrue();
        await using var freshProvider = ConfigureServices();
        var freshStorage = freshProvider.GetRequiredService<ManagedCode.Storage.Core.IStorage>();
        await Container.StopAsync(token);

        async Task ExpectFailureAsync(Func<CancellationToken, Task<bool>> operation)
        {
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(token);
            timeout.CancelAfter(TimeSpan.FromSeconds(2));
            (await operation(timeout.Token)).ShouldBeFalse();
        }

        await ExpectFailureAsync(async ct => (await Storage.UploadAsync("cannot upload", cancellationToken: ct)).IsSuccess);
        await ExpectFailureAsync(async ct => (await Storage.DownloadAsync("missing", ct)).IsSuccess);
        await ExpectFailureAsync(async ct => (await Storage.GetStreamAsync("missing", ct)).IsSuccess);
        await ExpectFailureAsync(async ct => (await Storage.GetBlobMetadataAsync("missing", ct)).IsSuccess);
        await ExpectFailureAsync(async ct => (await Storage.ExistsAsync("missing", ct)).IsSuccess);
        await ExpectFailureAsync(async ct => (await Storage.DeleteAsync("missing", ct)).IsSuccess);
        await ExpectFailureAsync(async ct => (await freshStorage.CreateContainerAsync(ct)).IsSuccess);
        await ExpectFailureAsync(async ct => (await Storage.RemoveContainerAsync(ct)).IsSuccess);
    }
}
