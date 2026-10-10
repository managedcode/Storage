using System;
using System.Threading.Tasks;
using ManagedCode.Storage.Core;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Xunit;
using ManagedCode.Storage.Sftp;
using ManagedCode.Storage.Sftp.Extensions;
using ManagedCode.Storage.Sftp.Options;

namespace ManagedCode.Storage.Tests.Storages.Registration;

public sealed class SftpRegistrationTests(SftpRegistrationHost host) : IClassFixture<SftpRegistrationHost>
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
        void Configure(SftpStorageOptions target)
        {
            target.Host = host.Host; target.Port = host.Port;
            target.Username = ManagedCode.Storage.Tests.Storages.Sftp.SftpContainerFactory.Username;
            target.Password = ManagedCode.Storage.Tests.Storages.Sftp.SftpContainerFactory.Password;
            target.RemoteDirectory = ManagedCode.Storage.Tests.Storages.Sftp.SftpContainerFactory.RemoteDirectory + "/" + bucket;
        }
        var services = new ServiceCollection();
        services.AddLogging();
        var options = new SftpStorageOptions();
        Configure(options);
        switch (registration)
        {
            case 0: services.AddSftpStorage(options); break;
            case 1: services.AddSftpStorage(Configure); break;
            case 2: services.AddSftpStorageAsDefault(options); break;
            case 3: services.AddSftpStorageAsDefault(Configure); break;
            case 4: services.AddSftpStorage("storage", Configure); break;
            case 5: services.AddSftpStorageAsDefault("storage", Configure); break;
        }
        await using var provider = services.BuildServiceProvider();
        var storage = registration >= 4
            ? provider.GetRequiredKeyedService<ISftpStorage>("storage")
            : provider.GetRequiredService<ISftpStorage>();
        await RegistrationRoundTrip.VerifyAsync(storage);
        if (registration is 2 or 3) await RegistrationRoundTrip.VerifyAsync(provider.GetRequiredService<IStorage>());
        if (registration == 5) provider.GetRequiredKeyedService<IStorage>("storage").ShouldBeSameAs(storage);
        (await storage.RemoveContainerAsync(TestContext.Current.CancellationToken)).IsSuccess.ShouldBeTrue();
    }
}
