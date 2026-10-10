using System;
using System.Linq;
using System.Threading.Tasks;
using DotNet.Testcontainers.Containers;
using ManagedCode.Storage.Tests.Common;
using ManagedCode.Storage.VirtualFileSystem.Core;
using ManagedCode.Storage.VirtualFileSystem.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Xunit;

namespace ManagedCode.Storage.Tests.Storages.Abstracts;

public abstract class RemoteVfsRangeContracts<T> : BaseContainer<T> where T : IContainer
{
    [Fact]
    public async Task RangeReads_RespectOffsetsShortReadsAndEofOnActualProviderStreams()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddVirtualFileSystem(Storage, options => options.DefaultContainer = string.Empty);
        await using var provider = services.BuildServiceProvider();
        var vfs = provider.GetRequiredService<IVirtualFileSystem>();
        var token = TestContext.Current.CancellationToken;
        var file = await vfs.GetFileAsync(new VfsPath("/range-contract.bin"), token);
        var content = new byte[131079];
        new Random(37).NextBytes(content);
        await file.WriteAllBytesAsync(content, cancellationToken: token);
        foreach (var (offset, count) in new[] { (0, 19), (3, 17), (81921, 21), (131075, 20), (131100, 4), (0, 0) })
            (await file.ReadRangeAsync(offset, count, token)).ShouldBe(content.Skip(offset).Take(count).ToArray());
        await Should.ThrowAsync<ArgumentOutOfRangeException>(async () => await file.ReadRangeAsync(-1, 1, token));
        await Should.ThrowAsync<ArgumentOutOfRangeException>(async () => await file.ReadRangeAsync(0, -1, token));
        (await file.ReadAllBytesAsync(token)).ShouldBe(content);
    }
}
