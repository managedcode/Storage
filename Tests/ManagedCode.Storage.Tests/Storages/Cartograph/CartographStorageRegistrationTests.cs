using System;
using System.IO;
using System.Threading.Tasks;
using ManagedCode.Storage.Cartograph;
using ManagedCode.Storage.Cartograph.Extensions;
using ManagedCode.Storage.Cartograph.Options;
using ManagedCode.Storage.Core;
using ManagedCode.Storage.Core.Extensions;
using ManagedCode.Storage.Core.Providers;
using ManagedCode.Storage.VirtualFileSystem.Core;
using ManagedCode.Storage.VirtualFileSystem.Extensions;
using Shouldly;
using Xunit;
using Microsoft.Extensions.DependencyInjection;

namespace ManagedCode.Storage.Tests.Storages.Cartograph;

public sealed class CartographStorageRegistrationTests
{
    [Fact]
    public async Task DefaultRegistration_WhenResolved_SharesTypedInstanceAndReadsFiles()
    {
        using var fixture = new CartographArtifactFixture();
        var services = new ServiceCollection();
        services.AddCartographStorageAsDefault(options => options.ArtifactPath = fixture.ArtifactPath);
        await using var provider = services.BuildServiceProvider();
        var storage = provider.GetRequiredService<IStorage>();
        storage.ShouldBeSameAs(provider.GetRequiredService<ICartographStorage>());
        await AssertReadAsync(storage, fixture);
    }

    [Fact]
    public async Task KeyedRegistration_WhenSeveralArtifactsRegistered_ResolvesEachAndEnablesFactory()
    {
        using var fixture = new CartographArtifactFixture();
        var second = fixture.WriteArtifact("second.ctg");
        var services = new ServiceCollection();
        services.AddStorageFactory();
        services.AddCartographStorageAsDefault("first", new CartographStorageOptions { ArtifactPath = fixture.ArtifactPath });
        services.AddCartographStorageAsDefault("second", options => options.ArtifactPath = second);
        await using var provider = services.BuildServiceProvider();
        var firstStorage = provider.GetRequiredKeyedService<IStorage>("first");
        var secondStorage = provider.GetRequiredKeyedService<IStorage>("second");
        firstStorage.ShouldBeSameAs(provider.GetRequiredKeyedService<ICartographStorage>("first"));
        secondStorage.ShouldBeSameAs(provider.GetRequiredKeyedService<ICartographStorage>("second"));
        ((ICartographStorage)secondStorage).StorageClient.ShouldBe(second);
        secondStorage.ShouldNotBeSameAs(firstStorage);
        await AssertReadAsync(firstStorage, fixture);
        await AssertReadAsync(secondStorage, fixture);
        using var created = provider.GetRequiredService<IStorageFactory>().CreateCartographStorage(second);
        await AssertReadAsync(created, fixture);
    }

    [Theory]
    [InlineData("path")]
    [InlineData("options")]
    [InlineData("configure")]
    [InlineData("core")]
    public async Task StorageFactory_WhenCartographRegistered_ConstructsReadableStorage(string overload)
    {
        using var fixture = new CartographArtifactFixture();
        var services = new ServiceCollection();
        services.AddStorageFactory();
        services.AddCartographStorage(options => options.ArtifactPath = fixture.ArtifactPath);
        await using var provider = services.BuildServiceProvider();
        var factory = provider.GetRequiredService<IStorageFactory>();
        using IStorage storage = overload switch
        {
            "path" => factory.CreateCartographStorage(fixture.ArtifactPath),
            "options" => factory.CreateCartographStorage(new CartographStorageOptions { ArtifactPath = fixture.ArtifactPath }),
            "configure" => factory.CreateCartographStorage(options => options.ArtifactPath = fixture.ArtifactPath),
            _ => factory.CreateStorage(new CartographStorageOptions { ArtifactPath = fixture.ArtifactPath })
        };
        await AssertReadAsync(storage, fixture);
        provider.GetService<IStorage>().ShouldBeNull();
    }

    [Fact]
    public async Task VirtualFileSystem_WhenUsingCartograph_ReadsCataloguedFiles()
    {
        using var fixture = new CartographArtifactFixture();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddCartographStorageAsDefault(options => options.ArtifactPath = fixture.ArtifactPath);
        services.AddVirtualFileSystem();
        await using var provider = services.BuildServiceProvider();
        await using var scope = provider.CreateAsyncScope();
        var vfs = scope.ServiceProvider.GetRequiredService<IVirtualFileSystem>();
        var path = new VfsPath("/reports/nested/дані.txt");
        (await vfs.FileExistsAsync(path)).ShouldBeTrue();
        var file = await vfs.GetFileAsync(path);
        (await file.ReadAllTextAsync()).ShouldBe(System.Text.Encoding.UTF8.GetString(fixture.Files["reports/nested/дані.txt"]));
    }

    private static async Task AssertReadAsync(IStorage storage, CartographArtifactFixture fixture)
    {
        var result = await storage.GetStreamAsync("root.txt");
        result.IsSuccess.ShouldBeTrue();
        await using var stream = result.Value.ShouldNotBeNull();
        var actual = new byte[fixture.Files["root.txt"].Length];
        await stream.ReadExactlyAsync(actual);
        actual.ShouldBe(fixture.Files["root.txt"]);
    }
}
