using System;
using System.Threading.Tasks;
using ManagedCode.Storage.Core;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Xunit;
using ManagedCode.Storage.Azure;
using ManagedCode.Storage.Azure.Extensions;
using ManagedCode.Storage.Azure.Options;

namespace ManagedCode.Storage.Tests.Storages.Registration;

public sealed class AzureRegistrationTests(AzureRegistrationHost host) : IClassFixture<AzureRegistrationHost>
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
        void Configure(AzureStorageOptions target)
        {
            target.Container = bucket; target.ConnectionString = host.ConnectionString;
        }
        var services = new ServiceCollection();
        services.AddLogging();
        var options = new AzureStorageOptions();
        Configure(options);
        switch (registration)
        {
            case 0: services.AddAzureStorage(options); break;
            case 1: services.AddAzureStorage(Configure); break;
            case 2: services.AddAzureStorageAsDefault(options); break;
            case 3: services.AddAzureStorageAsDefault(Configure); break;
            case 4: services.AddAzureStorage("storage", Configure); break;
            case 5: services.AddAzureStorageAsDefault("storage", Configure); break;
        }
        await using var provider = services.BuildServiceProvider();
        var storage = registration >= 4
            ? provider.GetRequiredKeyedService<IAzureStorage>("storage")
            : provider.GetRequiredService<IAzureStorage>();
        await RegistrationRoundTrip.VerifyAsync(storage);
        if (registration is 2 or 3) await RegistrationRoundTrip.VerifyAsync(provider.GetRequiredService<IStorage>());
        if (registration == 5) provider.GetRequiredKeyedService<IStorage>("storage").ShouldBeSameAs(storage);
        (await storage.RemoveContainerAsync(TestContext.Current.CancellationToken)).IsSuccess.ShouldBeTrue();
    }
}
