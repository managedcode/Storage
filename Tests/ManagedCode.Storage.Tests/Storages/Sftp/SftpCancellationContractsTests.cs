using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using ManagedCode.Storage.Core.Models;
using ManagedCode.Storage.Tests.Common;
using ManagedCode.Storage.Tests.Storages.Sftp;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Testcontainers.Sftp;
using Xunit;

namespace ManagedCode.Storage.Tests.Storages.Sftp;

public sealed class SftpCancellationContractsTests : BaseContainer<SftpContainer>
{
    protected override SftpContainer Build() => SftpContainerFactory.Create();
    protected override ServiceProvider ConfigureServices() => SftpConfigurator.ConfigureServices(
        Container.GetHost(), Container.GetPort(), SftpContainerFactory.Username,
        SftpContainerFactory.Password, SftpContainerFactory.RemoteDirectory);

    [Fact]
    public async Task CancellationDuringFileRead_StopsTheActualSftpUpload()
    {
        var token = TestContext.Current.CancellationToken;
        (await Storage.CreateContainerAsync(token)).IsSuccess.ShouldBeTrue();
        await using var file = await LargeFileTestHelper.CreateRandomFileAsync(16 * 1024 * 1024, cancellationToken: token);
        using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(token);
        await using var stream = new CancelAfterReadFileStream(file.FilePath, cancellation);
        var uploaded = await Storage.UploadAsync(stream, new UploadOptions { FileName = "canceled.bin" }, cancellation.Token);
        uploaded.IsFailed.ShouldBeTrue();
        stream.BytesRead.ShouldBeGreaterThan(0);
        stream.BytesRead.ShouldBeLessThan(file.FileInfo.Length);
        var metadata = await Storage.GetBlobMetadataAsync("canceled.bin", token);
        if (metadata.IsSuccess) metadata.Value!.Length.ShouldBeLessThan((ulong)file.FileInfo.Length);
    }

    // Real file input with a deterministic cancellation boundary; the SFTP SDK and server remain real.
    private sealed class CancelAfterReadFileStream(string path, CancellationTokenSource cancellation)
        : FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read)
    {
        public long BytesRead { get; private set; }
        private void AfterRead(int count) { BytesRead += count; if (count > 0) cancellation.Cancel(); }
        public override int Read(byte[] buffer, int offset, int count)
        { var read = base.Read(buffer, offset, count); AfterRead(read); return read; }
        public override int Read(Span<byte> buffer)
        { var read = base.Read(buffer); AfterRead(read); return read; }
        public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        { var read = await base.ReadAsync(buffer, cancellationToken); AfterRead(read); return read; }
        public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        { var read = await base.ReadAsync(buffer, offset, count, cancellationToken); AfterRead(read); return read; }
    }
}
