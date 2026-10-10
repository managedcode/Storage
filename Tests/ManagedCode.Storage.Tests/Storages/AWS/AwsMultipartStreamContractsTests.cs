using System;
using System.IO;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using DotNet.Testcontainers.Containers;
using ManagedCode.Storage.Aws;
using ManagedCode.Storage.Tests.Common;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Testcontainers.LocalStack;
using Xunit;

namespace ManagedCode.Storage.Tests.Storages.AWS;

public sealed class AwsMultipartStreamContractsTests : BaseContainer<LocalStackContainer>
{
    protected override LocalStackContainer Build() => AwsContainerFactory.Create();
    protected override ServiceProvider ConfigureServices() => AWSConfigurator.ConfigureServices(Container.GetConnectionString());

    private AmazonS3Client CreateClient() => new(new BasicAWSCredentials("localkey", "localsecret"),
        new AmazonS3Config { ServiceURL = Container.GetConnectionString(), ForcePathStyle = true });

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public async Task MultipartWrites_RoundTripBytesAcrossPartBoundaries(int constructor)
    {
        using var client = CreateClient();
        var token = TestContext.Current.CancellationToken;
        var bucket = "stream-contract-" + Guid.NewGuid().ToString("N");
        await client.PutBucketAsync(bucket, token);
        var content = new byte[5 * 1024 * 1024 + 257];
        new Random(19).NextBytes(content);
        var key = "multipart.bin";
        var uri = new Uri($"s3://{bucket}/{key}");
        var stream = constructor switch
        {
            0 => new BlobStream(client, bucket, key),
            1 => new BlobStream(client, uri),
            _ => new BlobStream(client, uri.ToString())
        };
        stream.CanRead.ShouldBeFalse();
        stream.CanSeek.ShouldBeFalse();
        stream.CanWrite.ShouldBeTrue();
        stream.SetLength(content.Length);
        stream.Write(content, 0, 5 * 1024 * 1024);
        stream.Flush();
        stream.Write(content, 5 * 1024 * 1024, 257);
        stream.Write(content, 0, 0);
        stream.Length.ShouldBe(content.Length);
        stream.Position.ShouldBe(content.Length);
        stream.Dispose();
        stream.Dispose();
        stream.CanWrite.ShouldBeFalse();
        using var downloaded = await client.GetObjectAsync(bucket, key, token);
        downloaded.ContentLength.ShouldBe(content.Length);
        (await SHA256.HashDataAsync(downloaded.ResponseStream, token)).ShouldBe(SHA256.HashData(content));
        var pending = await client.ListMultipartUploadsAsync(new ListMultipartUploadsRequest { BucketName = bucket }, token);
        (pending.MultipartUploads?.Count ?? 0).ShouldBe(0);
    }

    [Fact]
    public async Task EmptyStream_CreatesEmptyObjectWithoutLeavingMultipartUpload()
    {
        using var client = CreateClient();
        var token = TestContext.Current.CancellationToken;
        var bucket = "empty-contract-" + Guid.NewGuid().ToString("N");
        await client.PutBucketAsync(bucket, token);
        using (var stream = new BlobStream(client, bucket, "empty"))
            stream.Write(Array.Empty<byte>(), 0, 0);
        using var downloaded = await client.GetObjectAsync(bucket, "empty", token);
        downloaded.ContentLength.ShouldBe(0);
        var pending = await client.ListMultipartUploadsAsync(new ListMultipartUploadsRequest { BucketName = bucket }, token);
        (pending.MultipartUploads?.Count ?? 0).ShouldBe(0);
    }

    [Fact]
    public async Task InvalidAndDisposedWrites_RejectWithoutChangingStoredContent()
    {
        using var client = CreateClient();
        var token = TestContext.Current.CancellationToken;
        var bucket = "validation-contract-" + Guid.NewGuid().ToString("N");
        await client.PutBucketAsync(bucket, token);
        var stream = new BlobStream(client, bucket, "file");
        Should.Throw<ArgumentNullException>(() => stream.Write(null!, 0, 1));
        Should.Throw<ArgumentOutOfRangeException>(() => stream.Write(new byte[2], -1, 1));
        Should.Throw<ArgumentOutOfRangeException>(() => stream.Write(new byte[2], 0, -1));
        Should.Throw<ArgumentException>(() => stream.Write(new byte[2], 1, 2));
        Should.Throw<NotSupportedException>(() => stream.Read(new byte[1], 0, 1));
        Should.Throw<NotSupportedException>(() => stream.Seek(0, SeekOrigin.Begin));
        Should.Throw<NotSupportedException>(() => stream.Position = 0);
        stream.Write(new byte[] { 7, 8 }, 0, 2);
        stream.Dispose();
        Should.Throw<ObjectDisposedException>(() => stream.Write(new byte[] { 9 }, 0, 1));
        using var downloaded = await client.GetObjectAsync(bucket, "file", token);
        downloaded.ContentLength.ShouldBe(2);
    }
}
