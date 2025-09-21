namespace ManagedCode.Storage.Tests.VirtualFileSystem.Fixtures;

public sealed record VirtualFileSystemCapabilities(
    bool Enabled = true,
    bool SupportsListing = true,
    bool SupportsDirectoryDelete = true,
    bool SupportsDirectoryCopy = true,
    bool SupportsMove = true,
    bool SupportsDirectoryStats = true);
