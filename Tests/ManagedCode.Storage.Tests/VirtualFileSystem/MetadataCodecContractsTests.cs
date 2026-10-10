using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using ManagedCode.Storage.Tests.VirtualFileSystem;
using ManagedCode.Storage.VirtualFileSystem.Extensions;
using ManagedCode.Storage.VirtualFileSystem.Metadata;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Xunit;

namespace ManagedCode.Storage.Tests.VirtualFileSystem;

public sealed class MetadataCodecContractsTests
{
    [Fact]
    public async Task ProtectedProviderCodec_RoundTripsFieldsWithoutLeakingReservedKeys()
    {
        await using var session = await LocalVfsSession.CreateAsync();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddVirtualFileSystem(session.Storage);
        await using var provider = services.BuildServiceProvider();
        var manager = provider.GetRequiredService<IMetadataManager>();
        var metadata = new VfsMetadata
        {
            Version = "1.0",
            Created = new DateTimeOffset(2024, 1, 2, 3, 4, 5, TimeSpan.FromHours(2)),
            Modified = new DateTimeOffset(2025, 6, 7, 8, 9, 10, TimeSpan.Zero)
        };
        metadata.CustomMetadata["kind"] = "content";
        var custom = new Dictionary<string, string> { ["category"] = "contract", ["x-vfs-prefixed"] = "value" };
        var encoded = Invoke<Dictionary<string, string>>(manager, "BuildMetadataDictionary", metadata, custom);
        var decoded = Invoke<VfsMetadata>(manager, "ParseVfsMetadata", encoded);
        decoded.Version.ShouldBe(metadata.Version);
        decoded.Created.ShouldBe(metadata.Created);
        decoded.Modified.ShouldBe(metadata.Modified);
        decoded.CustomMetadata.Count.ShouldBe(1);
        decoded.CustomMetadata["kind"].ShouldBe("content");
        var extra = Invoke<Dictionary<string, string>>(manager, "ExtractCustomMetadata", encoded);
        extra.Count.ShouldBe(2);
        extra["category"].ShouldBe("contract");
        extra["prefixed"].ShouldBe("value");
    }

    [Fact]
    public async Task MissingOrMalformedOptionalFields_AreHandledWithoutDiscardingUserMetadata()
    {
        await using var session = await LocalVfsSession.CreateAsync();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddVirtualFileSystem(session.Storage);
        await using var provider = services.BuildServiceProvider();
        var manager = provider.GetRequiredService<IMetadataManager>();
        Invoke<VfsMetadata?>(manager, "ParseVfsMetadata", new Dictionary<string, string>()).ShouldBeNull();
        var encoded = new Dictionary<string, string>
        {
            ["x-vfs-vfs-version"] = "1.0",
            ["x-vfs-vfs-created"] = "invalid",
            ["x-vfs-vfs-modified"] = "invalid",
            ["x-vfs-vfs-attributes"] = "invalid",
            ["public"] = "outside",
            ["x-vfs-extra"] = "inside"
        };
        var parsed = Invoke<VfsMetadata>(manager, "ParseVfsMetadata", encoded);
        parsed.Version.ShouldBe("1.0");
        parsed.CustomMetadata.ShouldBeEmpty();
        var extra = Invoke<Dictionary<string, string>>(manager, "ExtractCustomMetadata", encoded);
        extra["public"].ShouldBe("outside");
        extra["extra"].ShouldBe("inside");
    }

    // Exercise the protected codec extension points on the real DI-resolved metadata manager.
    // No storage or metadata implementation is replaced by a fake.
    private static T Invoke<T>(IMetadataManager manager, string name, params object?[] arguments)
        => (T)typeof(BaseMetadataManager).GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic)!
            .Invoke(manager, arguments)!;
}
