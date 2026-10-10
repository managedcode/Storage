using ManagedCode.Storage.Tests.Common;
using ManagedCode.Storage.Tests.Storages.AWS;
using ManagedCode.Storage.Tests.Storages.Azure;
using ManagedCode.Storage.Tests.Storages.GCS;
using ManagedCode.Storage.Tests.Storages.Sftp;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.Azurite;
using Testcontainers.FakeGcsServer;
using Testcontainers.LocalStack;
using Testcontainers.Sftp;

namespace ManagedCode.Storage.Tests.Storages.Registration;

public sealed class AwsRegistrationHost : BaseContainer<LocalStackContainer>
{
    public string Endpoint => Container.GetConnectionString();
    protected override LocalStackContainer Build() => AwsContainerFactory.Create();
    protected override ServiceProvider ConfigureServices() => AWSConfigurator.ConfigureServices(Endpoint);
}

public sealed class AzureRegistrationHost : BaseContainer<AzuriteContainer>
{
    public string ConnectionString => Container.GetConnectionString();
    protected override AzuriteContainer Build() => new AzuriteBuilder(ContainerImages.Azurite).WithCommand("--skipApiVersionCheck").Build();
    protected override ServiceProvider ConfigureServices() => AzureConfigurator.ConfigureServices(ConnectionString);
}

public sealed class GcsRegistrationHost : BaseContainer<FakeGcsServerContainer>
{
    public string Endpoint => Container.GetConnectionString();
    protected override FakeGcsServerContainer Build() => GcsContainerFactory.Create();
    protected override ServiceProvider ConfigureServices() => GCSConfigurator.ConfigureServices(Endpoint);
}

public sealed class SftpRegistrationHost : BaseContainer<SftpContainer>
{
    public string Host => Container.GetHost();
    public int Port => Container.GetPort();
    protected override SftpContainer Build() => SftpContainerFactory.Create();
    protected override ServiceProvider ConfigureServices() => SftpConfigurator.ConfigureServices(Host, Port,
        SftpContainerFactory.Username, SftpContainerFactory.Password, SftpContainerFactory.RemoteDirectory);
}
