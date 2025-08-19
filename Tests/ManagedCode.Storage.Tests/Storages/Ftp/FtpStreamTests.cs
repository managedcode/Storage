using ManagedCode.Storage.Tests.Storages.Abstracts;
using Microsoft.Extensions.DependencyInjection;

namespace ManagedCode.Storage.Tests.Storages.Ftp;

/// <summary>
/// Stream tests for FTP storage.
/// </summary>
public class FtpStreamTests : StreamTests<FtpContainer>
{
    protected override FtpContainer Build()
    {
        return new FtpContainer();
    }

    protected override ServiceProvider ConfigureServices()
    {
        return FtpConfigurator.ConfigureServices(
            Container.GetHost(),
            Container.GetPort(),
            FtpContainer.Username,
            FtpContainer.Password,
            "/test-container");
    }
}