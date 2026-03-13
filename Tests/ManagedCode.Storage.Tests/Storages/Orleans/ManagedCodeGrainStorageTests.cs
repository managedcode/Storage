using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using ManagedCode.Storage.Core;
using ManagedCode.Storage.FileSystem;
using ManagedCode.Storage.FileSystem.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Orleans;
using Orleans.Hosting;
using Orleans.Providers;
using Orleans.Runtime;
using Orleans.Serialization.Activators;
using Orleans.Serialization.Serializers;
using Orleans.Storage;
using Shouldly;
using Xunit;

namespace ManagedCode.Storage.Tests.Storages.Orleans;

public class ManagedCodeGrainStorageTests
{
    [Fact]
    public void AddGrainStorageAsDefault_Generic_ShouldResolveDefaultProvider()
    {
        var root = CreateTempDirectory("default");

        try
        {
            var services = CreateServices();
            services.AddFileSystemStorageAsDefault(options => options.BaseFolder = root);
            services.AddGrainStorageAsDefault<IFileSystemStorage>(options =>
            {
                options.GrainStorageSerializer = TestGrainStorageSerializer.Instance;
            });

            using var provider = services.BuildServiceProvider();

            var defaultProvider = provider.GetRequiredService<IGrainStorage>();
            var namedProvider = provider.GetRequiredKeyedService<IGrainStorage>(ProviderConstants.DEFAULT_STORAGE_PROVIDER_NAME);

            defaultProvider.ShouldBeSameAs(namedProvider);
        }
        finally
        {
            Cleanup(root);
        }
    }

    [Fact]
    public void AddGrainStorage_WithSiloBuilder_ShouldRegisterNamedProvider()
    {
        var root = CreateTempDirectory("builder");

        try
        {
            var builder = new TestSiloBuilder();
            ConfigureInfrastructure(builder.Services);
            builder.Services.AddFileSystemStorageAsDefault(options => options.BaseFolder = root);

            builder.AddGrainStorage<IFileSystemStorage>("builder-store", options =>
            {
                options.GrainStorageSerializer = TestGrainStorageSerializer.Instance;
            });

            using var provider = builder.Services.BuildServiceProvider();

            provider.GetRequiredKeyedService<IGrainStorage>("builder-store").ShouldNotBeNull();
        }
        finally
        {
            Cleanup(root);
        }
    }

    [Fact]
    public async Task AddGrainStorage_KeyedStorage_ShouldUseConfiguredKeyedIStorage()
    {
        var tenantARoot = CreateTempDirectory("tenant-a");
        var tenantBRoot = CreateTempDirectory("tenant-b");

        try
        {
            var services = CreateServices();
            services.AddFileSystemStorageAsDefault("tenant-a", options => options.BaseFolder = tenantARoot);
            services.AddFileSystemStorageAsDefault("tenant-b", options => options.BaseFolder = tenantBRoot);
            services.AddGrainStorage("profiles", "tenant-a", options =>
            {
                options.GrainStorageSerializer = TestGrainStorageSerializer.Instance;
                options.PathBuilder = context => $"states/{Sanitize(context.GrainId)}.json";
            });

            using var provider = services.BuildServiceProvider();
            var grainStorage = provider.GetRequiredKeyedService<IGrainStorage>("profiles");
            var grainId = GrainId.Create("user", "42");
            var state = new GrainState<TestState>(new TestState
            {
                Name = "Ada",
                Count = 5
            });

            await grainStorage.WriteStateAsync("profile", grainId, state);

            File.Exists(Path.Combine(tenantARoot, "states", $"{Sanitize(grainId)}.json")).ShouldBeTrue();
            File.Exists(Path.Combine(tenantBRoot, "states", $"{Sanitize(grainId)}.json")).ShouldBeFalse();
        }
        finally
        {
            Cleanup(tenantARoot);
            Cleanup(tenantBRoot);
        }
    }

    [Fact]
    public async Task ManagedCodeGrainStorage_ReadWriteClear_ShouldRoundTripState()
    {
        var root = CreateTempDirectory("roundtrip");

        try
        {
            var services = CreateServices();
            services.AddFileSystemStorageAsDefault(options => options.BaseFolder = root);
            services.AddGrainStorageAsDefault<IFileSystemStorage>(options =>
            {
                options.GrainStorageSerializer = TestGrainStorageSerializer.Instance;
                options.PathBuilder = context => $"orleans-tests/{context.ProviderName}/{context.StateName}/{Sanitize(context.GrainId)}.state";
            });

            using var provider = services.BuildServiceProvider();
            var grainStorage = provider.GetRequiredService<IGrainStorage>();
            var grainId = GrainId.Create("user", "alpha");
            var state = new GrainState<TestState>(new TestState
            {
                Name = "initial",
                Count = 3
            });

            await grainStorage.WriteStateAsync("profile", grainId, state);

            state.RecordExists.ShouldBeTrue();
            state.ETag.ShouldNotBeNullOrWhiteSpace();

            var reloaded = new GrainState<TestState>();
            await grainStorage.ReadStateAsync("profile", grainId, reloaded);

            reloaded.RecordExists.ShouldBeTrue();
            reloaded.ETag.ShouldBe(state.ETag);
            reloaded.State.Name.ShouldBe("initial");
            reloaded.State.Count.ShouldBe(3);

            var previousEtag = reloaded.ETag;
            reloaded.State.Count = 4;
            await grainStorage.WriteStateAsync("profile", grainId, reloaded);

            reloaded.ETag.ShouldNotBe(previousEtag);

            await grainStorage.ClearStateAsync("profile", grainId, reloaded);
            reloaded.RecordExists.ShouldBeFalse();

            var cleared = new GrainState<TestState>();
            await grainStorage.ReadStateAsync("profile", grainId, cleared);

            cleared.RecordExists.ShouldBeFalse();
            cleared.State.ShouldNotBeNull();
            cleared.State.Name.ShouldBe(string.Empty);
            cleared.State.Count.ShouldBe(0);
        }
        finally
        {
            Cleanup(root);
        }
    }

    [Fact]
    public async Task ManagedCodeGrainStorage_Write_WhenEtagMismatches_ShouldThrow()
    {
        var root = CreateTempDirectory("etag");

        try
        {
            var services = CreateServices();
            services.AddFileSystemStorageAsDefault(options => options.BaseFolder = root);
            services.AddGrainStorageAsDefault<IFileSystemStorage>(options =>
            {
                options.GrainStorageSerializer = TestGrainStorageSerializer.Instance;
            });

            using var provider = services.BuildServiceProvider();
            var grainStorage = provider.GetRequiredService<IGrainStorage>();
            var grainId = GrainId.Create("user", "conflict");

            var first = new GrainState<TestState>(new TestState
            {
                Name = "first",
                Count = 1
            });

            await grainStorage.WriteStateAsync("profile", grainId, first);

            var latest = new GrainState<TestState>();
            await grainStorage.ReadStateAsync("profile", grainId, latest);
            latest.State.Name = "latest";
            await grainStorage.WriteStateAsync("profile", grainId, latest);

            var stale = new GrainState<TestState>(new TestState
            {
                Name = "stale",
                Count = 2
            })
            {
                ETag = first.ETag,
                RecordExists = true
            };

            await Should.ThrowAsync<InconsistentStateException>(() =>
                grainStorage.WriteStateAsync("profile", grainId, stale));
        }
        finally
        {
            Cleanup(root);
        }
    }

    private static ServiceCollection CreateServices()
    {
        var services = new ServiceCollection();
        ConfigureInfrastructure(services);
        return services;
    }

    private static void ConfigureInfrastructure(IServiceCollection services)
    {
        services.AddLogging();
        services.AddSingleton<IActivatorProvider, TestActivatorProvider>();
    }

    private static string CreateTempDirectory(string suffix)
    {
        var path = Path.Combine(Path.GetTempPath(), "ManagedCode.Storage.Orleans.Tests", suffix, Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }

    private static void Cleanup(string path)
    {
        if (Directory.Exists(path))
        {
            Directory.Delete(path, recursive: true);
        }
    }

    private static string Sanitize(GrainId grainId)
    {
        return grainId.ToString().Replace('/', '_');
    }

    private sealed class TestSiloBuilder : ISiloBuilder
    {
        public IServiceCollection Services { get; } = new ServiceCollection();

        public IConfiguration Configuration { get; } = new ConfigurationManager();
    }

    private sealed class TestActivatorProvider : IActivatorProvider
    {
        public IActivator<T> GetActivator<T>()
        {
            return new TestActivator<T>();
        }
    }

    private sealed class TestActivator<T> : IActivator<T>
    {
        public T Create()
        {
            return Activator.CreateInstance<T>();
        }
    }

    private sealed class TestGrainStorageSerializer : IGrainStorageSerializer
    {
        public static TestGrainStorageSerializer Instance { get; } = new();

        private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

        public BinaryData Serialize<T>(T input)
        {
            return new BinaryData(JsonSerializer.SerializeToUtf8Bytes(input, SerializerOptions));
        }

        public T Deserialize<T>(BinaryData input)
        {
            return JsonSerializer.Deserialize<T>(input.ToArray(), SerializerOptions)
                   ?? throw new InvalidOperationException($"Unable to deserialize {typeof(T).FullName}.");
        }
    }

    private sealed class TestState
    {
        public string Name { get; set; } = string.Empty;

        public int Count { get; set; }
    }
}
