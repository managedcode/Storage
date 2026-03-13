using System;
using System.Threading;
using System.Threading.Tasks;
using Google.Cloud.Storage.V1;
using ManagedCode.Communication;
using ManagedCode.Storage.FileSystem;
using ManagedCode.Storage.FileSystem.Options;
using ManagedCode.Storage.Google;
using ManagedCode.Storage.Google.Options;

namespace ManagedCode.Storage.TestFakes;

public class FakeGoogleStorage : FileSystemStorage, IGCPStorage
{
    public FakeGoogleStorage() : base(new FileSystemStorageOptions())
    {
    }

    public new StorageClient StorageClient { get; } = null!;

    public Task<Result> SetStorageOptions(GCPStorageOptions options, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(Result.Succeed());
    }

    public Task<Result> SetStorageOptions(Action<GCPStorageOptions> options, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(Result.Succeed());
    }
}
