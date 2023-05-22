using Azure.Storage.Files.DataLake.Models;
using ManagedCode.Storage.Core;

namespace ManagedCode.Storage.AzureDataLake.Options;

public class AzureDataLakeStorageOptions : IStorageOptions
{
    public string ConnectionString { get; set; }
    public string FileSystem { get; set; }

    public bool CreateContainerIfNotExists { get; set; } = true;

    public DataLakeFileSystemCreateOptions PublicAccessType { get; set; } = new()
    {
        PublicAccessType = Azure.Storage.Files.DataLake.Models.PublicAccessType.None
    };
}