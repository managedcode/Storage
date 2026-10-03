using ManagedCode.Storage.Cartograph.Options;
using ManagedCode.Storage.Core;

namespace ManagedCode.Storage.Cartograph;

/// <summary>Reads catalogued files from an immutable Cartograph artifact.</summary>
public interface ICartographStorage : IStorage<string, CartographStorageOptions>
{
}
