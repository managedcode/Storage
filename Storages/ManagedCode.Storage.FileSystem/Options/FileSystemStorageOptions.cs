using ManagedCode.Storage.Core;

namespace ManagedCode.Storage.FileSystem.Options;

public class FileSystemStorageOptions : IStorageOptions
{
    public string? BaseFolder { get; set; }

    public bool CreateContainerIfNotExists { get; set; } = true;
}