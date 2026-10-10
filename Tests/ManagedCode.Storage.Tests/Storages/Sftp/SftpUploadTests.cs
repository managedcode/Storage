using System.Threading.Tasks;
using ManagedCode.Storage.Tests.Storages.Abstracts;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.Sftp;
using Xunit;

namespace ManagedCode.Storage.Tests.Storages.Sftp;

/// <summary>
/// Upload tests for SFTP storage.
/// </summary>
public class SftpUploadTests : UploadTests<SftpContainer>
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

    [Fact]
    public override async Task UploadAsync_WithCancellationToken_BigFile_ShouldCancel()
    {
        await base.UploadAsync_WithCancellationToken_BigFile_ShouldCancel();
    }
}
