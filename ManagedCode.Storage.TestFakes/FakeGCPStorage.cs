using Google.Cloud.Storage.V1;
using ManagedCode.Storage.FileSystem;
using ManagedCode.Storage.FileSystem.Options;
using ManagedCode.Storage.Gcp;

namespace ManagedCode.Storage.TestFakes;

public class FakeGCPStorage : FileSystemStorage, IGCPStorage
{
    public FakeGCPStorage() : base(new FileSystemStorageOptions())
    {
    }

    public StorageClient StorageClient { get; }
}