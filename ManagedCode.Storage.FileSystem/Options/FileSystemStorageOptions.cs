using ManagedCode.Storage.Core;

namespace ManagedCode.Storage.FileSystem.Options;

public class FileSystemStorageOptions : StorageOptions
{
    public string? BaseFolder { get; set; }
}