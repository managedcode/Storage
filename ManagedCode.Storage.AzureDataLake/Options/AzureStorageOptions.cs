using Azure.Storage.Files.DataLake.Models;
using ManagedCode.Storage.Core;

namespace ManagedCode.Storage.AzureDataLake.Options;

public class AzureDataLakeStorageOptions : StorageOptions
{
    public string ConnectionString { get; set; }
    public string FileSystem { get; set; }
    public PublicAccessType PublicAccessType { get; set; } = PublicAccessType.None;
}