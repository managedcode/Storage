using System;
using System.Threading.Tasks;
using Amazon.S3;
using Amazon.S3.Model;
using ManagedCode.Storage.Aws;
using ManagedCode.Storage.Aws.Options;
using ManagedCode.Storage.Core.Models;
using ManagedCode.Storage.Tests.Storages.Registration;
using Shouldly;
using Xunit;

namespace ManagedCode.Storage.Tests.Storages.AWS;

public sealed class AwsLegalHoldContractsTests(AwsRegistrationHost host) : IClassFixture<AwsRegistrationHost>
{
    [Fact]
    public async Task ObjectLockLegalHold_CanBeSetReadAndClearedThroughTheRealS3Sdk()
    {
        var bucket = "hold-" + Guid.NewGuid().ToString("N");
        var configuration = new AmazonS3Config { ServiceURL = host.Endpoint, ForcePathStyle = true };
        using var sdk = new AmazonS3Client("localkey", "localsecret", configuration);
        var token = TestContext.Current.CancellationToken;
        await sdk.PutBucketAsync(new PutBucketRequest { BucketName = bucket, ObjectLockEnabledForBucket = true }, token);
        using var storage = new AWSStorage(new AWSStorageOptions
        { Bucket = bucket, PublicKey = "localkey", SecretKey = "localsecret", OriginalOptions = configuration });
        (await storage.UploadAsync("held content", new UploadOptions { FileName = "held.txt" }, token)).IsSuccess.ShouldBeTrue();
        (await storage.SetLegalHoldAsync(true, "held.txt", token)).IsSuccess.ShouldBeTrue();
        (await storage.HasLegalHoldAsync("held.txt", token)).Value.ShouldBeTrue();
        (await storage.SetLegalHoldAsync(false, "held.txt", token)).IsSuccess.ShouldBeTrue();
        (await storage.HasLegalHoldAsync("held.txt", token)).Value.ShouldBeFalse();
        (await storage.SetLegalHoldAsync(true, "missing.txt", token)).IsFailed.ShouldBeTrue();
        var metadata = await sdk.GetObjectMetadataAsync(bucket, "held.txt", token);
        await sdk.DeleteObjectAsync(new DeleteObjectRequest { BucketName = bucket, Key = "held.txt", VersionId = metadata.VersionId }, token);
        (await storage.RemoveContainerAsync(token)).IsSuccess.ShouldBeTrue();
    }
}
