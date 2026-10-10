using System;
using System.Threading.Tasks;
using ManagedCode.Storage.Core;
using ManagedCode.Storage.Core.Models;
using ManagedCode.Storage.Tests.VirtualFileSystem;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Orleans.Configuration;
using Orleans;
using Orleans.Runtime;
using Orleans.Serialization.Activators;
using Orleans.Serialization.Serializers;
using Orleans.Storage;
using Shouldly;
using Xunit;

namespace ManagedCode.Storage.Tests.Storages.Orleans;

[Collection(ManagedCodeOrleansClusterCollection.Name)]
public sealed class OrleansStateRuntimeContractsTests(ManagedCodeOrleansClusterFixture fixture)
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Clear_PreservesItsConfiguredPersistenceAndConcurrencyContract(bool delete)
    {
        await using var session = await LocalVfsSession.CreateAsync();
        var options = CreateOptions("state.json");
        options.DeleteStateOnClear = delete;
        var provider = CreateProvider(session.Storage, options);
        var id = GrainId.Create("coverage", Guid.NewGuid().ToString("N"));
        var state = new GrainState<ManagedCodeStorageBackedState>(new() { Value = "persisted", WriteCount = 3 });
        await provider.ReadStateAsync("profile", id, state);
        state.State!.Value.ShouldBe(string.Empty);
        state.RecordExists.ShouldBeFalse();
        state.State!.Value = "persisted";
        await provider.WriteStateAsync("profile", id, state);
        var stale = new GrainState<ManagedCodeStorageBackedState>(new()) { ETag = "stale" };
        await Should.ThrowAsync<InconsistentStateException>(() => provider.ClearStateAsync("profile", id, stale));
        await Should.ThrowAsync<InconsistentStateException>(() => provider.WriteStateAsync("profile", id, new GrainState<ManagedCodeStorageBackedState>(new())));
        await provider.ClearStateAsync("profile", id, state);
        state.RecordExists.ShouldBeFalse();
        state.State.Value.ShouldBe(string.Empty);
        (await session.Storage.ExistsAsync("state.json", TestContext.Current.CancellationToken)).Value.ShouldBe(!delete);
        var reload = new GrainState<ManagedCodeStorageBackedState>(new());
        await provider.ReadStateAsync("profile", id, reload);
        reload.RecordExists.ShouldBeFalse();
        reload.ETag.ShouldBe(state.ETag);
        reload.State!.Value = "after-clear";
        await provider.WriteStateAsync("profile", id, reload);
        await provider.ReadStateAsync("profile", id, state);
        state.State.Value.ShouldBe("after-clear");
    }

    [Theory]
    [InlineData("leaf.state")]
    [InlineData("/nested\\state.json")]
    [InlineData("")]
    public async Task Paths_RoundTripWithoutDependingOnActivationLifetime(string path)
    {
        await using var session = await LocalVfsSession.CreateAsync();
        var provider = CreateProvider(session.Storage, CreateOptions(path));
        var id = GrainId.Create("tenant/entity", "id/with spaces");
        var state = new GrainState<ManagedCodeStorageBackedState>(new() { Value = "Привіт" });
        await provider.WriteStateAsync("profile/preferences", id, state);
        var reloaded = new GrainState<ManagedCodeStorageBackedState>(new());
        await provider.ReadStateAsync("profile/preferences", id, reloaded);
        reloaded.State!.Value.ShouldBe("Привіт");
        await provider.ClearStateAsync("profile/preferences", id, reloaded);
    }

    [Fact]
    public async Task CorruptPersistentBytes_PropagateReadWriteAndClearFailures()
    {
        await using var session = await LocalVfsSession.CreateAsync();
        (await session.Storage.UploadAsync("invalid serialized payload", new UploadOptions("corrupt.state"), TestContext.Current.CancellationToken)).IsSuccess.ShouldBeTrue();
        var provider = CreateProvider(session.Storage, CreateOptions("corrupt.state"));
        var id = GrainId.Create("coverage", "corrupt");
        var state = new GrainState<ManagedCodeStorageBackedState>(new());
        await Should.ThrowAsync<Exception>(() => provider.ReadStateAsync("profile", id, state));
        await Should.ThrowAsync<Exception>(() => provider.WriteStateAsync("profile", id, state));
        await Should.ThrowAsync<Exception>(() => provider.ClearStateAsync("profile", id, state));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public async Task InvalidStoragePath_PropagatesFailureInsteadOfResettingOrAcknowledgingState(int operation)
    {
        await using var session = await LocalVfsSession.CreateAsync();
        var provider = CreateProvider(session.Storage, CreateOptions("../outside.state"));
        var id = GrainId.Create("coverage", "invalid-path");
        var state = new GrainState<ManagedCodeStorageBackedState>(new());
        await Should.ThrowAsync<InvalidOperationException>(() => operation switch
        {
            0 => provider.ReadStateAsync("profile", id, state),
            1 => provider.WriteStateAsync("profile", id, state),
            _ => provider.ClearStateAsync("profile", id, state)
        });
        state.RecordExists.ShouldBeFalse();
    }

    private ManagedCodeStorageGrainStorageOptions CreateOptions(string path)
    {
        var configured = fixture.GetSiloServiceProvider().GetRequiredService<IOptionsMonitor<ManagedCodeStorageGrainStorageOptions>>().Get(ManagedCodeOrleansProviderNames.FileSystem);
        return new() { GrainStorageSerializer = configured.GrainStorageSerializer, PathBuilder = _ => path };
    }

    private ManagedCodeGrainStorage CreateProvider(IStorage storage, ManagedCodeStorageGrainStorageOptions options) => new(
        "coverage", options, storage, fixture.GetSiloServiceProvider().GetRequiredService<IActivatorProvider>(), NullLogger<ManagedCodeGrainStorage>.Instance);
}
