using System;
using System.Threading.Tasks;
using ManagedCode.Storage.Core;
using ManagedCode.Storage.FileSystem;
using ManagedCode.Storage.Tests.VirtualFileSystem;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Orleans;
using Orleans.Configuration;
using Orleans.Hosting;
using Orleans.Providers;
using Orleans.Runtime;
using Orleans.Serialization.Serializers;
using Orleans.Storage;
using Shouldly;
using Xunit;

namespace ManagedCode.Storage.Tests.Storages.Orleans;

[Collection(ManagedCodeOrleansClusterCollection.Name)]
public sealed class OrleansRegistrationRuntimeContractsTests(ManagedCodeOrleansClusterFixture fixture)
{
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    [InlineData(6)]
    [InlineData(7)]
    [InlineData(8)]
    [InlineData(9)]
    public async Task RealSiloBuilderRegistrations_ResolveTheCorrectStorageAndPersistState(int mode)
    {
        await using var session = await LocalVfsSession.CreateAsync();
        var services = fixture.GetSiloServiceProvider();
        var serializer = services.GetRequiredService<IOptionsMonitor<ManagedCodeStorageGrainStorageOptions>>().Get(ManagedCodeOrleansProviderNames.FileSystem).GrainStorageSerializer;
        void Configure(ManagedCodeStorageGrainStorageOptions options)
        {
            options.GrainStorageSerializer = serializer;
            options.PathBuilder = _ => "record.state";
            if (mode == 3 || mode == 8) options.StorageFactory = provider => provider.GetRequiredService<IFileSystemStorage>();
        }
        using var host = new HostBuilder().ConfigureLogging(logging => logging.SetMinimumLevel(LogLevel.Warning)).UseOrleans(builder =>
        {
            builder.ConfigureServices(s =>
            {
                s.AddSingleton<IStorage>(session.Storage);
                s.AddSingleton<IFileSystemStorage>(session.Storage);
                s.AddKeyedSingleton<IStorage>("tenant", session.Storage);
                s.AddKeyedSingleton<IFileSystemStorage>("tenant", session.Storage);
                s.AddSingleton(services.GetRequiredService<IActivatorProvider>());
            });
            Register(builder, mode, Configure);
        }).Build();
        var name = mode < 5 ? ProviderConstants.DEFAULT_STORAGE_PROVIDER_NAME : "named";
        var provider = host.Services.GetRequiredKeyedService<IGrainStorage>(name);
        if (mode < 5) host.Services.GetRequiredService<IGrainStorage>().ShouldBeSameAs(provider);
        var id = GrainId.Create("coverage", Guid.NewGuid().ToString("N"));
        var state = new GrainState<ManagedCodeStorageBackedState>(new() { Value = "registered" });
        await provider.WriteStateAsync("profile", id, state);
        await provider.ReadStateAsync("profile", id, state);
        state.State!.Value.ShouldBe("registered");
        (await session.Storage.ExistsAsync("record.state", TestContext.Current.CancellationToken)).Value.ShouldBeTrue();
        await provider.ClearStateAsync("profile", id, state);
    }

    private static void Register(ISiloBuilder builder, int mode, Action<ManagedCodeStorageGrainStorageOptions> configure)
    {
        switch (mode)
        {
            case 0: builder.AddGrainStorageAsDefault<IFileSystemStorage>(configure); break;
            case 1: builder.AddGrainStorageAsDefault<IFileSystemStorage>("tenant", configure); break;
            case 2: builder.AddGrainStorageAsDefault("tenant", configure); break;
            case 3: builder.AddGrainStorageAsDefault(configure); break;
            case 4: builder.AddGrainStorageAsDefault((OptionsBuilder<ManagedCodeStorageGrainStorageOptions> options) => options.Configure(configure)); break;
            case 5: builder.AddGrainStorage<IFileSystemStorage>("named", configure); break;
            case 6: builder.AddGrainStorage<IFileSystemStorage>("named", "tenant", configure); break;
            case 7: builder.AddGrainStorage("named", "tenant", configure); break;
            case 8: builder.AddGrainStorage("named", configure); break;
            case 9: builder.AddGrainStorage("named", (OptionsBuilder<ManagedCodeStorageGrainStorageOptions> options) => options.Configure(configure)); break;
            default: throw new ArgumentOutOfRangeException(nameof(mode));
        }
    }
}
