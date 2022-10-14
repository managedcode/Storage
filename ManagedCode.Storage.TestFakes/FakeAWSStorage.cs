using Amazon.S3;
using ManagedCode.Storage.Aws;
using ManagedCode.Storage.FileSystem;
using ManagedCode.Storage.FileSystem.Options;

namespace ManagedCode.Storage.TestFakes;

public class FakeAWSStorage : FileSystemStorage, IAWSStorage
{
    public FakeAWSStorage() : base(new FileSystemStorageOptions())
    {
    }

    public IAmazonS3 StorageClient { get; }
}