using Google.Cloud.Storage.V1;
using ManagedCode.Storage.Core;

namespace ManagedCode.Storage.Gcp;

public interface IGCPStorage : IStorage<StorageClient>
{
}