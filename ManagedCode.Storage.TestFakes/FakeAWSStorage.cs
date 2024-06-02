using System;
using System.Threading;
using System.Threading.Tasks;
using Amazon.S3;
using ManagedCode.Communication;
using ManagedCode.Storage.Aws;
using ManagedCode.Storage.Aws.Options;
using ManagedCode.Storage.FileSystem;
using ManagedCode.Storage.FileSystem.Options;

namespace ManagedCode.Storage.TestFakes;

public class FakeAWSStorage : FileSystemStorage, IAWSStorage
{
    public FakeAWSStorage() : base(new FileSystemStorageOptions())
    {
    }

    public IAmazonS3 StorageClient { get; }

    public Task<Result> SetStorageOptions(AWSStorageOptions options, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(Result.Succeed());
    }

    public Task<Result> SetStorageOptions(Action<AWSStorageOptions> options, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(Result.Succeed());
    }
}