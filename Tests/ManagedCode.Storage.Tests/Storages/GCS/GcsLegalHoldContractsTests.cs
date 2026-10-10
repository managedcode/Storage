using System;
using System.Threading.Tasks;
using ManagedCode.Storage.Core.Models;
using ManagedCode.Storage.Google;
using ManagedCode.Storage.Google.Options;
using ManagedCode.Storage.Tests.Storages.Registration;
using Shouldly;
using Xunit;

namespace ManagedCode.Storage.Tests.Storages.GCS;

public sealed class GcsLegalHoldContractsTests(GcsRegistrationHost host) : IClassFixture<GcsRegistrationHost>
{
    [Fact]
    public async Task UnsupportedTemporaryHoldBackend_DoesNotReportAnUnappliedHoldAsSuccessful()
    {
        using var storage = new GCPStorage(new GCPStorageOptions
        {
            BucketOptions = new BucketOptions { Bucket = "hold-" + Guid.NewGuid().ToString("N"), ProjectId = "hold-contract" },
            StorageClientBuilder = new global::Google.Cloud.Storage.V1.StorageClientBuilder { UnauthenticatedAccess = true, BaseUri = host.Endpoint }
        });
        var token = TestContext.Current.CancellationToken;
        (await storage.UploadAsync("held content", new UploadOptions { FileName = "held.txt" }, token)).IsSuccess.ShouldBeTrue();
        (await storage.SetLegalHoldAsync(true, "held.txt", token)).IsFailed.ShouldBeTrue();
        (await storage.HasLegalHoldAsync("held.txt", token)).Value.ShouldBeFalse();
        (await storage.SetLegalHoldAsync(false, "held.txt", token)).IsSuccess.ShouldBeTrue();
        (await storage.HasLegalHoldAsync("held.txt", token)).Value.ShouldBeFalse();
        (await storage.SetLegalHoldAsync(true, "missing.txt", token)).IsFailed.ShouldBeTrue();
        (await storage.DeleteAsync("held.txt", token)).IsSuccess.ShouldBeTrue();
        (await storage.RemoveContainerAsync(token)).IsSuccess.ShouldBeTrue();
    }
}
