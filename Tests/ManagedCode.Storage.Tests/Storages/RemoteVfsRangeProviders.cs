using ManagedCode.Storage.Tests.Common;
using ManagedCode.Storage.Tests.Storages.Abstracts;
using ManagedCode.Storage.Tests.Storages.AWS;
using ManagedCode.Storage.Tests.Storages.Azure;
using ManagedCode.Storage.Tests.Storages.GCS;
using ManagedCode.Storage.Tests.Storages.Sftp;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.Azurite;
using Testcontainers.FakeGcsServer;
using Testcontainers.LocalStack;
using Testcontainers.Sftp;

namespace ManagedCode.Storage.Tests.Storages;

public sealed class AwsRemoteVfsRangeTests : RemoteVfsRangeContracts<LocalStackContainer>
{
    protected override LocalStackContainer Build() => AwsContainerFactory.Create();
    protected override ServiceProvider ConfigureServices() => AWSConfigurator.ConfigureServices(Container.GetConnectionString());
}

public sealed class AzureRemoteVfsRangeTests : RemoteVfsRangeContracts<AzuriteContainer>
{
    protected override AzuriteContainer Build() => new AzuriteBuilder(ContainerImages.Azurite).WithCommand("--skipApiVersionCheck").Build();
    protected override ServiceProvider ConfigureServices() => AzureConfigurator.ConfigureServices(Container.GetConnectionString());
}

public sealed class GcsRemoteVfsRangeTests : RemoteVfsRangeContracts<FakeGcsServerContainer>
{
    protected override FakeGcsServerContainer Build() => GcsContainerFactory.Create();
    protected override ServiceProvider ConfigureServices() => GCSConfigurator.ConfigureServices(Container.GetConnectionString());
}

public sealed class SftpRemoteVfsRangeTests : RemoteVfsRangeContracts<SftpContainer>
{
    protected override SftpContainer Build() => SftpContainerFactory.Create();
    protected override ServiceProvider ConfigureServices() => SftpConfigurator.ConfigureServices(Container.GetHost(), Container.GetPort(),
        SftpContainerFactory.Username, SftpContainerFactory.Password, SftpContainerFactory.RemoteDirectory);
}
