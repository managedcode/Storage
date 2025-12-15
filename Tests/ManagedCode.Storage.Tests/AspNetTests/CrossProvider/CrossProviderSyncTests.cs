using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using ManagedCode.MimeTypes;
using ManagedCode.Storage.Aws;
using ManagedCode.Storage.Azure;
using ManagedCode.Storage.Core;
using ManagedCode.Storage.Core.Helpers;
using ManagedCode.Storage.Core.Models;
using ManagedCode.Storage.FileSystem;
using ManagedCode.Storage.Google;
using ManagedCode.Storage.Tests.Common;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Xunit;

namespace ManagedCode.Storage.Tests.AspNetTests.CrossProvider;

[Collection(nameof(StorageTestApplication))]
public class CrossProviderSyncTests(StorageTestApplication testApplication)
{
    public static IEnumerable<object[]> ProviderPairs()
    {
        yield return new object[] { "azure", "aws" };
        yield return new object[] { "azure", "filesystem" };
        yield return new object[] { "filesystem", "azure" };
    }

    [Theory]
    [MemberData(nameof(ProviderPairs))]
    public async Task SyncBlobAcrossProviders_PreservesPayloadAndMetadata(string sourceKey, string targetKey)
    {
        await using var scope = testApplication.Services.CreateAsyncScope();
        var services = scope.ServiceProvider;

        var sourceStorage = ResolveStorage(sourceKey, services);
        var targetStorage = ResolveStorage(targetKey, services);

        await EnsureContainerAsync(sourceStorage);
        await EnsureContainerAsync(targetStorage);

	        var payload = new byte[256 * 1024];
	        RandomNumberGenerator.Fill(payload);

	        using var crcStream = new MemoryStream(payload, writable: false);
	        var expectedCrc = Crc32Helper.CalculateStreamCrc(crcStream);

        var directory = $"sync-tests/{Guid.NewGuid():N}";
        var fileName = $"payload-{Guid.NewGuid():N}.bin";
        var mimeType = MimeHelper.GetMimeType(fileName);
        var metadata = new Dictionary<string, string>
        {
            ["source"] = sourceKey,
            ["target"] = targetKey,
            ["scenario"] = "cross-provider-sync"
        };

        await using (var sourceStream = new MemoryStream(payload, writable: false))
        {
            var sourceUpload = await sourceStorage.UploadAsync(
                sourceStream,
                new UploadOptions(fileName, directory, mimeType, metadata),
                CancellationToken.None);

            sourceUpload.IsSuccess.ShouldBeTrue();
            sourceUpload.Value.ShouldNotBeNull();

            var sourceBlobName = ResolveBlobName(sourceUpload.Value!, fileName, directory);

            var sourceStreamResult = await sourceStorage.GetStreamAsync(sourceBlobName, CancellationToken.None);
            sourceStreamResult.IsSuccess.ShouldBeTrue();
            sourceStreamResult.Value.ShouldNotBeNull();

            await using var sourceBlobStream = sourceStreamResult.Value!;

            var targetMetadata = new Dictionary<string, string>(metadata)
            {
                ["mirroredFrom"] = sourceBlobName
            };

            var targetUpload = await targetStorage.UploadAsync(
                sourceBlobStream,
                new UploadOptions(fileName, directory + "-mirror", mimeType, targetMetadata),
                CancellationToken.None);

            targetUpload.IsSuccess.ShouldBeTrue();
            targetUpload.Value.ShouldNotBeNull();

            var targetBlobName = ResolveBlobName(targetUpload.Value!, fileName, directory + "-mirror");

            var targetDownload = await targetStorage.DownloadAsync(targetBlobName, CancellationToken.None);
            targetDownload.IsSuccess.ShouldBeTrue();
            targetDownload.Value.ShouldNotBeNull();

            await using var mirroredLocalFile = targetDownload.Value!;
            var actualCrc = LargeFileTestHelper.CalculateFileCrc(mirroredLocalFile.FilePath);
            actualCrc.ShouldBe(expectedCrc);

            targetUpload.Value!.Length.ShouldBe((ulong)payload.Length);
            targetUpload.Value!.MimeType.ShouldBe(mimeType);

            var targetMetadataStored = targetUpload.Value!.Metadata;
            if (targetMetadataStored is not null)
            {
                targetMetadataStored.ShouldContainKeyAndValue("mirroredFrom", sourceBlobName);
            }

            var deleteSource = await sourceStorage.DeleteAsync(sourceBlobName, CancellationToken.None);
            deleteSource.IsSuccess.ShouldBeTrue();

            var deleteTarget = await targetStorage.DeleteAsync(targetBlobName, CancellationToken.None);
            deleteTarget.IsSuccess.ShouldBeTrue();
        }
    }

    private static async Task EnsureContainerAsync(IStorage storage)
    {
        var result = await storage.CreateContainerAsync(CancellationToken.None);
        result.IsSuccess.ShouldBeTrue();
    }

    private static IStorage ResolveStorage(string providerKey, IServiceProvider services)
    {
        return providerKey switch
        {
            "azure" => services.GetRequiredService<IAzureStorage>(),
            "aws" => services.GetRequiredService<IAWSStorage>(),
            "filesystem" => services.GetRequiredService<IFileSystemStorage>(),
            "gcp" => services.GetRequiredService<IGCPStorage>(),
            _ => throw new ArgumentOutOfRangeException(nameof(providerKey), providerKey, "Unknown provider")
        };
    }

    private static string ResolveBlobName(BlobMetadata metadata, string fileName, string directory)
    {
        if (!string.IsNullOrWhiteSpace(metadata.FullName))
        {
            return metadata.FullName!;
        }

        if (!string.IsNullOrWhiteSpace(metadata.Name))
        {
            return string.IsNullOrWhiteSpace(directory)
                ? metadata.Name!
                : Combine(directory, metadata.Name!);
        }

        return string.IsNullOrWhiteSpace(directory)
            ? fileName
            : Combine(directory, fileName);
    }

    private static string Combine(string directory, string file)
    {
        return string.IsNullOrWhiteSpace(directory)
            ? file
            : $"{directory.TrimEnd('/')}/{file}";
    }
}
