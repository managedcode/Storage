using System;
using System.Threading;
using System.Threading.Tasks;
using ManagedCode.Storage.Core;
using Microsoft.Extensions.DependencyInjection;
using Orleans.Storage;
using Shouldly;
using Xunit;

namespace ManagedCode.Storage.Tests.Storages.Orleans;

[Collection(ManagedCodeOrleansClusterCollection.Name)]
public sealed class ManagedCodeGrainStorageClusterTests(ManagedCodeOrleansClusterFixture fixture)
{
    [Theory]
    [MemberData(nameof(AllProviders))]
    public void Silo_ShouldRegisterGrainStorageProvider_ForEachStorage(string providerName)
    {
        var siloServices = fixture.GetSiloServiceProvider();
        siloServices.GetRequiredKeyedService<IGrainStorage>(providerName).ShouldNotBeNull();
    }

    [Theory]
    [MemberData(nameof(AllProviders))]
    public async Task Grain_ShouldActivateAndPersistState_UsingConfiguredStorage(string providerName)
    {
        var grainKey = $"{providerName}-{Guid.NewGuid():N}";
        var expectedValue = $"{providerName}-value-{Guid.NewGuid():N}";
        var grain = fixture.GetGrain(providerName, grainKey);

        await grain.SetValueAsync(expectedValue);

        (await grain.GetValueAsync()).ShouldBe(expectedValue);

        var storagePath = await grain.GetStoragePathAsync();
        var existsResult = await fixture.ResolveStorage(providerName).ExistsAsync(storagePath, CancellationToken.None);

        existsResult.IsSuccess.ShouldBeTrue();
        existsResult.Value.ShouldBeTrue();
    }

    [Theory]
    [MemberData(nameof(AllProviders))]
    public async Task Grain_Clear_ShouldDeletePersistedState_FromConfiguredStorage(string providerName)
    {
        var grainKey = $"{providerName}-clear-{Guid.NewGuid():N}";
        var grain = fixture.GetGrain(providerName, grainKey);
        var storage = fixture.ResolveStorage(providerName);
        var storagePath = await grain.GetStoragePathAsync();

        await grain.SetValueAsync("to-clear");
        await grain.ReloadAsync();
        await grain.ClearAsync();

        (await grain.GetValueAsync()).ShouldBe(string.Empty);

        var existsAfterClear = await storage.ExistsAsync(storagePath, CancellationToken.None);
        existsAfterClear.IsSuccess.ShouldBeTrue();
        existsAfterClear.Value.ShouldBeFalse();
    }

    public static TheoryData<string> AllProviders => ManagedCodeOrleansClusterTestMatrix.AllProviders;
}
