using Azure.Storage.Files.DataLake.Models;
using ManagedCode.Storage.Core;

namespace ManagedCode.Storage.Azure.DataLake.Options;

public class AzureDataLakeStorageOptions : IStorageOptions
{
    public string ConnectionString { get; set; } = string.Empty;
    public string FileSystem { get; set; } = string.Empty;

    public DataLakeFileSystemCreateOptions PublicAccessType { get; set; } = new()
    {
        PublicAccessType = global::Azure.Storage.Files.DataLake.Models.PublicAccessType.None
    };

    public bool CreateContainerIfNotExists { get; set; } = true;
}
