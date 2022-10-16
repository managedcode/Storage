using Google.Cloud.Storage.V1;
using ManagedCode.Communication;
using ManagedCode.Storage.FileSystem;
using ManagedCode.Storage.FileSystem.Options;
using ManagedCode.Storage.Gcp;
using ManagedCode.Storage.Gcp.Options;

namespace ManagedCode.Storage.TestFakes;

public class FakeGCPStorage : FileSystemStorage, IGCPStorage
{
    public FakeGCPStorage() : base(new FileSystemStorageOptions())
    {
    }

    public StorageClient StorageClient { get; }

    public Task<Result> SetStorageOptions(GCPStorageOptions options, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(Result.Succeed());
    }

    public Task<Result> SetStorageOptions(Action<GCPStorageOptions> options, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(Result.Succeed());
    }
}