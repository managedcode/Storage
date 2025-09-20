using Xunit;

namespace ManagedCode.Storage.Tests.VirtualFileSystem;

[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class VirtualFileSystemCollection
{
    public const string Name = "VirtualFileSystem";
}
