using System;
using ManagedCode.Storage.Cartograph.Options;
using ManagedCode.Storage.Core.Providers;

namespace ManagedCode.Storage.Cartograph.Extensions;

public static class StorageFactoryExtensions
{
    public static ICartographStorage CreateCartographStorage(this IStorageFactory factory, string artifactPath) =>
        factory.CreateCartographStorage(new CartographStorageOptions { ArtifactPath = artifactPath });

    public static ICartographStorage CreateCartographStorage(this IStorageFactory factory, CartographStorageOptions options) =>
        factory.CreateStorage<ICartographStorage, CartographStorageOptions>(options);

    public static ICartographStorage CreateCartographStorage(this IStorageFactory factory, Action<CartographStorageOptions> configure) =>
        factory.CreateStorage<ICartographStorage, CartographStorageOptions>(configure);
}
