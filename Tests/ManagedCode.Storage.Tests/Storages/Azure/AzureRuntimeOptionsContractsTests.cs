using System;
using System.IO;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using Azure.Identity;
using ManagedCode.Storage.Azure;
using ManagedCode.Storage.Azure.Options;
using ManagedCode.Storage.Core;
using ManagedCode.Storage.Core.Models;
using ManagedCode.Storage.FileSystem.Options;
using ManagedCode.Storage.Tests.Storages.Registration;
using Shouldly;
using Xunit;

namespace ManagedCode.Storage.Tests.Storages.Azure;

public sealed class AzureRuntimeOptionsContractsTests(AzureRegistrationHost host) : IClassFixture<AzureRegistrationHost>
{
    [Fact]
    public async Task OptionsAndWriteStreams_TargetTheNewContainerAndKeepOriginalOptionsIndependent()
    {
        var token = TestContext.Current.CancellationToken;
        var original = new AzureStorageOptions { ConnectionString = host.ConnectionString, Container = "options-" + Guid.NewGuid().ToString("N"), CreateContainerIfNotExists = true };
        var storage = (AzureStorage)AzureStorageConnection.Create(host.ConnectionString, original.Container);
        await storage.StorageClient.CreateIfNotExistsAsync(cancellationToken: token);
        (await storage.CreateContainerAsync(token)).IsSuccess.ShouldBeTrue();
        (await storage.SetStorageOptions((IStorageOptions)original, token)).IsSuccess.ShouldBeTrue();
        var second = "options-" + Guid.NewGuid().ToString("N");
        (await storage.SetStorageOptions((IStorageOptions options) => ((AzureStorageOptions)options).Container = second, token)).IsSuccess.ShouldBeTrue();
        original.Container.ShouldNotBe(second);
        storage.StorageClient.Name.ShouldBe(second);
        var bytes = RandomNumberGenerator.GetBytes(4107);
        var opened = await storage.OpenWriteStreamAsync("stream.bin", token);
        opened.IsSuccess.ShouldBeTrue(opened.Problem?.ToString());
        await using (var write = opened.Value!) await write.WriteAsync(bytes, token);
        var read = await storage.GetStreamAsync("stream.bin", token);
        read.IsSuccess.ShouldBeTrue();
        await using (var stream = read.Value!)
        {
            await using var output = new MemoryStream();
            await stream.CopyToAsync(output, token);
            output.ToArray().ShouldBe(bytes);
        }
        (await storage.SetStorageOptions((IStorageOptions)new FileSystemStorageOptions(), token)).IsFailed.ShouldBeTrue();
        (await storage.SetLegalHoldAsync(true, "missing.bin", token)).IsFailed.ShouldBeTrue();
        (await storage.HasLegalHoldAsync("missing.bin", token)).IsFailed.ShouldBeTrue();
        (await storage.RemoveContainerAsync(token)).IsSuccess.ShouldBeTrue();
        (await ((AzureStorage)AzureStorageConnection.Create(host.ConnectionString, original.Container)).RemoveContainerAsync(token)).IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public async Task CredentialOptionsCallback_PreservesSdkReferencesAndKeepsTheOriginalIndependent()
    {
        var token = new CancellationToken(canceled: true);
        var credential = new EnvironmentCredential();
        var original = new AzureStorageCredentialsOptions
        {
            ServiceUri = new Uri("https://account.blob.core.windows.net/"),
            ContainerName = "original",
            Container = "original",
            Credentials = credential
        };
        var storage = new AzureStorage(original);
        var result = await storage.SetStorageOptions((IStorageOptions options) =>
        {
            var copy = options.ShouldBeOfType<AzureStorageCredentialsOptions>();
            copy.ShouldNotBeSameAs(original);
            copy.Credentials.ShouldBeSameAs(credential);
            copy.ServiceUri.ShouldBe(original.ServiceUri);
            copy.Container = "changed";
            copy.ContainerName = "changed";
        }, token);
        result.IsFailed.ShouldBeTrue();
        original.ContainerName.ShouldBe("original");
        storage.StorageClient.Uri.AbsoluteUri.ShouldBe("https://account.blob.core.windows.net/changed");
    }

    [Theory]
    [InlineData("http://example.invalid/")]
    [InlineData("https://example.invalid/path")]
    [InlineData("https://example.invalid/?key=value")]
    [InlineData("https://example.invalid/#fragment")]
    public void CredentialEndpoint_MustBeAnHttpsOrigin(string endpoint) =>
        Should.Throw<ArgumentException>(() => AzureStorageConnection.Create(endpoint, "files", new EnvironmentCredential()));

    [Fact]
    public void ValidCredentialEndpoint_PreservesTheActualSdkCredentialWithoutAuthenticating()
    {
        var credential = new EnvironmentCredential();
        var storage = (AzureStorage)AzureStorageConnection.Create("https://account.blob.core.windows.net/", "files", credential);
        storage.StorageClient.Uri.AbsoluteUri.ShouldBe("https://account.blob.core.windows.net/files");
        Should.Throw<ArgumentNullException>(() => AzureStorageConnection.Create("https://account.blob.core.windows.net/", "files"));
        Should.Throw<ArgumentException>(() => AzureStorageConnection.Create(" ", "files"));
        Should.Throw<ArgumentException>(() => AzureStorageConnection.Create("https://account.blob.core.windows.net/", " ", credential));
    }
}
