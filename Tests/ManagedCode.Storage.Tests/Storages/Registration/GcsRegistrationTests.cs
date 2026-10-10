using System;
using System.Threading.Tasks;
using ManagedCode.Storage.Core;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Xunit;
using ManagedCode.Storage.Google;
using ManagedCode.Storage.Google.Extensions;
using ManagedCode.Storage.Google.Options;

namespace ManagedCode.Storage.Tests.Storages.Registration;

public sealed class GcsRegistrationTests(GcsRegistrationHost host) : IClassFixture<GcsRegistrationHost>
{
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    public async Task RegistrationOverloads_ResolveProvidersThatPersistBytes(int registration)
    {
        var bucket = "registration-" + Guid.NewGuid().ToString("N");
        void Configure(GCPStorageOptions target)
        {
            target.BucketOptions = new BucketOptions { Bucket = bucket, ProjectId = "registration-contract" };
            target.StorageClientBuilder = new global::Google.Cloud.Storage.V1.StorageClientBuilder { UnauthenticatedAccess = true, BaseUri = host.Endpoint };
        }
        var services = new ServiceCollection();
        services.AddLogging();
        var options = new GCPStorageOptions();
        Configure(options);
        switch (registration)
        {
            case 0: services.AddGCPStorage(options); break;
            case 1: services.AddGCPStorage(Configure); break;
            case 2: services.AddGCPStorageAsDefault(options); break;
            case 3: services.AddGCPStorageAsDefault(Configure); break;
            case 4: services.AddGCPStorage("storage", Configure); break;
            case 5: services.AddGCPStorageAsDefault("storage", Configure); break;
        }
        await using var provider = services.BuildServiceProvider();
        var storage = registration >= 4
            ? provider.GetRequiredKeyedService<IGCPStorage>("storage")
            : provider.GetRequiredService<IGCPStorage>();
        await RegistrationRoundTrip.VerifyAsync(storage);
        if (registration is 2 or 3) await RegistrationRoundTrip.VerifyAsync(provider.GetRequiredService<IStorage>());
        if (registration == 5) provider.GetRequiredKeyedService<IStorage>("storage").ShouldBeSameAs(storage);
        (await storage.RemoveContainerAsync(TestContext.Current.CancellationToken)).IsSuccess.ShouldBeTrue();
    }
}
