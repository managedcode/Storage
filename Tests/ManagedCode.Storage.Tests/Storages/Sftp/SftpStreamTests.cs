using ManagedCode.Storage.Tests.Storages.Abstracts;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.Sftp;

namespace ManagedCode.Storage.Tests.Storages.Sftp;

/// <summary>
/// Stream tests for SFTP storage.
/// </summary>
public class SftpStreamTests : StreamTests<SftpContainer>
{
    protected override SftpContainer Build() => SftpContainerFactory.Create();

    protected override ServiceProvider ConfigureServices()
    {
        return SftpConfigurator.ConfigureServices(
            Container.GetHost(),
            Container.GetPort(),
            SftpContainerFactory.Username,
            SftpContainerFactory.Password,
            SftpContainerFactory.RemoteDirectory);
    }
}
