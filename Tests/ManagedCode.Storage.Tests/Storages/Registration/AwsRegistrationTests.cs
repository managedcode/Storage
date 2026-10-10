using System;
using System.Threading.Tasks;
using ManagedCode.Storage.Core;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Xunit;
using ManagedCode.Storage.Aws;
using ManagedCode.Storage.Aws.Extensions;
using ManagedCode.Storage.Aws.Options;

namespace ManagedCode.Storage.Tests.Storages.Registration;

public sealed class AwsRegistrationTests(AwsRegistrationHost host) : IClassFixture<AwsRegistrationHost>
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
        void Configure(AWSStorageOptions target)
        {
            target.Bucket = bucket;
            target.PublicKey = "localkey"; target.SecretKey = "localsecret";
            target.OriginalOptions = new Amazon.S3.AmazonS3Config { ServiceURL = host.Endpoint };
        }
        var services = new ServiceCollection();
        services.AddLogging();
        var options = new AWSStorageOptions();
        Configure(options);
        switch (registration)
        {
            case 0: services.AddAWSStorage(options); break;
            case 1: services.AddAWSStorage(Configure); break;
            case 2: services.AddAWSStorageAsDefault(options); break;
            case 3: services.AddAWSStorageAsDefault(Configure); break;
            case 4: services.AddAWSStorage("storage", Configure); break;
            case 5: services.AddAWSStorageAsDefault("storage", Configure); break;
        }
        await using var provider = services.BuildServiceProvider();
        var storage = registration >= 4
            ? provider.GetRequiredKeyedService<IAWSStorage>("storage")
            : provider.GetRequiredService<IAWSStorage>();
        await RegistrationRoundTrip.VerifyAsync(storage);
        if (registration is 2 or 3) await RegistrationRoundTrip.VerifyAsync(provider.GetRequiredService<IStorage>());
        if (registration == 5) provider.GetRequiredKeyedService<IStorage>("storage").ShouldBeSameAs(storage);
        (await storage.RemoveContainerAsync(TestContext.Current.CancellationToken)).IsSuccess.ShouldBeTrue();
    }
}
