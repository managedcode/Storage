using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ManagedCode.Storage.Core.Models;
using ManagedCode.Storage.Tests.Storages.Abstracts;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Testcontainers.Sftp;
using Xunit;

namespace ManagedCode.Storage.Tests.Storages.Sftp;

/// <summary>
/// Blob tests for SFTP storage.
/// </summary>
public class SftpBlobTests : BlobTests<SftpContainer>
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
    public async Task RelativeDirectoryListing_UsesTheConfiguredRemoteRoot()
    {
        var token = TestContext.Current.CancellationToken;
        (await Storage.UploadAsync("content", new UploadOptions("report.txt") { Directory = "nested" }, token))
            .IsSuccess.ShouldBeTrue();

        var files = await Storage.GetBlobMetadataListAsync("nested", token).ToListAsync(token);

        files.Select(file => file.FullName).ShouldBe(["nested/report.txt"]);
        (await Storage.GetStreamAsync("nested/report.txt", new CancellationToken(canceled: true))).IsFailed.ShouldBeTrue();
    }
}
