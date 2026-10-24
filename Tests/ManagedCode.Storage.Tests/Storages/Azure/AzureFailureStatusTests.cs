using System;
using System.IO;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Azure;
using ManagedCode.Communication;
using ManagedCode.Storage.Azure;
using ManagedCode.Storage.Azure.Options;
using ManagedCode.Storage.Tests.Storages.Registration;
using Shouldly;
using Xunit;

namespace ManagedCode.Storage.Tests.Storages.Azure;

public sealed class AzureFailureStatusTests(AzureRegistrationHost host) : IClassFixture<AzureRegistrationHost>
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task StreamRead_MissingWriteReadDeleteMissing_PreservesProviderStatus(bool direct)
    {
        var token = TestContext.Current.CancellationToken;
        using var storage = (AzureStorage)AzureStorageConnection.Create(host.ConnectionString, "status-" + Guid.NewGuid().ToString("N"));
        await storage.StorageClient.CreateIfNotExistsAsync(cancellationToken: token);
        const string path = "artifact.bin";
        Task<Result<Stream>> ReadAsync() => direct ? storage.OpenReadStreamAsync(path, token) : storage.GetStreamAsync(path, token);
        var absent = await ReadAsync();
        absent.IsFailed.ShouldBeTrue();
        absent.Problem!.StatusCode.ShouldBe(404);
        absent.Problem.ErrorCode.ShouldBe("BlobNotFound");
        var bytes = RandomNumberGenerator.GetBytes(4107);
        var write = await storage.OpenWriteStreamAsync(path, token);
        write.IsSuccess.ShouldBeTrue(write.Problem?.ToString());
        await using (var stream = write.Value!)
        { await stream.WriteAsync(bytes, token); }
        var read = await ReadAsync();
        read.IsSuccess.ShouldBeTrue(read.Problem?.ToString());
        await using (var stream = read.Value!)
        {
            await using var copy = new MemoryStream();
            await stream.CopyToAsync(copy, token);
            copy.ToArray().ShouldBe(bytes);
        }
        (await storage.GetBlobMetadataAsync(path, token)).Value!.Length.ShouldBe((ulong)bytes.Length);
        (await storage.DeleteAsync(path, token)).Value.ShouldBeTrue();
        (await ReadAsync()).Problem!.StatusCode.ShouldBe(404);
        (await storage.GetBlobMetadataAsync(path, token)).Problem!.StatusCode.ShouldBe(404);
        (await storage.DownloadAsync(path, token)).Problem!.StatusCode.ShouldBe(404);
        (await storage.RemoveContainerAsync(token)).IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public async Task InvalidCredentials_PreserveTheActualAzureAuthenticationFailure()
    {
        var token = TestContext.Current.CancellationToken;
        var connection = Regex.Replace(host.ConnectionString, @"(?<=AccountKey=)[^;]+",
            Convert.ToBase64String(RandomNumberGenerator.GetBytes(64)), RegexOptions.IgnoreCase);
        using var storage = new AzureStorage(new AzureStorageOptions
        { ConnectionString = connection, Container = "denied-" + Guid.NewGuid().ToString("N") });
        var native = await Should.ThrowAsync<RequestFailedException>(() => storage.StorageClient.GetPropertiesAsync(cancellationToken: token));
        native.Status.ShouldBe(403);
        var result = await storage.GetStreamAsync("artifact.bin", token);
        result.IsFailed.ShouldBeTrue();
        result.Problem!.StatusCode.ShouldBe(native.Status);
        result.Problem.ErrorCode.ShouldBe(native.ErrorCode);
    }
}
