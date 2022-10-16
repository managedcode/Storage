using Google.Cloud.Storage.V1;
using ManagedCode.Storage.Core;
using ManagedCode.Storage.Gcp.Options;

namespace ManagedCode.Storage.Gcp;

public interface IGCPStorage : IStorage<StorageClient, GCPStorageOptions>
{
}