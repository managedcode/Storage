using Google.Cloud.Storage.V1;
using ManagedCode.Storage.Core;
using ManagedCode.Storage.Google.Options;

namespace ManagedCode.Storage.Google;

public interface IGCPStorage : IStorage<StorageClient, GCPStorageOptions>
{
}