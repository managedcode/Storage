using Amazon.S3;
using ManagedCode.Storage.Core;

namespace ManagedCode.Storage.Aws;

public interface IAWSStorage : IStorage<IAmazonS3>
{
}