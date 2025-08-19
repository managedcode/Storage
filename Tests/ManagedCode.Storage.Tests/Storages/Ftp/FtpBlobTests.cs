using ManagedCode.Storage.Tests.Storages.Abstracts;
using Microsoft.Extensions.DependencyInjection;

namespace ManagedCode.Storage.Tests.Storages.Ftp;

/// <summary>
/// Blob tests for FTP storage.
/// </summary>
public class FtpBlobTests : BlobTests<FtpContainer>
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