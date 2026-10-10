using System;
using System.Buffers.Binary;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using ManagedCode.Storage.Core.Models;
using ManagedCode.Storage.Core.Providers;
using ManagedCode.Storage.Core;
using ManagedCode.Storage.Core.Extensions;
using ManagedCode.Storage.Sftp;
using ManagedCode.Storage.Sftp.Extensions;
using ManagedCode.Storage.Sftp.Options;
using ManagedCode.Storage.Tests.Common;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Testcontainers.Sftp;
using Xunit;

namespace ManagedCode.Storage.Tests.Storages.Sftp;

public sealed class SftpPrivateKeyRuntimeContractsTests : IAsyncLifetime
{
    private string _directory = null!;
    private string _privateKey = null!;
    private SftpContainer _container = null!;

    public async ValueTask InitializeAsync()
    {
        _directory = Path.Combine(Path.GetTempPath(), "storage-ephemeral-ssh-key", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_directory);
        using var rsa = RSA.Create(2048);
        _privateKey = rsa.ExportRSAPrivateKeyPem();
        await File.WriteAllTextAsync(Path.Combine(_directory, "private.pem"), _privateKey, TestContext.Current.CancellationToken);
        var key = rsa.ExportParameters(false);
        using var wire = new MemoryStream();
        WriteSshField(wire, Encoding.ASCII.GetBytes("ssh-rsa"));
        WriteSshField(wire, key.Exponent!);
        WriteSshField(wire, new byte[] { 0 }.Concat(key.Modulus!).ToArray());
        var publicFile = Path.Combine(_directory, "public.pub");
        await File.WriteAllTextAsync(publicFile, "ssh-rsa " + Convert.ToBase64String(wire.ToArray()), TestContext.Current.CancellationToken);
        _container = new SftpBuilder(ContainerImages.Sftp).WithUsername(SftpContainerFactory.Username).WithPassword(SftpContainerFactory.Password)
            .WithUploadDirectory(SftpContainerFactory.UploadMountDirectory).WithResourceMapping(new FileInfo(publicFile), "/home/storage/.ssh/keys/").Build();
        await _container.StartAsync(TestContext.Current.CancellationToken);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public async Task RealPasswordAndPrivateKeyFactories_RoundTripAndRecursivelyRemoveTheirContainer(int mode)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddStorageFactory();
        services.AddSftpStorage(options => { options.Host = _container.GetHost(); options.Port = _container.GetPort(); options.Username = SftpContainerFactory.Username; options.Password = SftpContainerFactory.Password; });
        await using var provider = services.BuildServiceProvider();
        var factory = provider.GetRequiredService<IStorageFactory>();
        var directory = "/upload/keys-" + Guid.NewGuid().ToString("N");
        var storage = mode switch
        {
            0 => factory.CreateSftpStorageWithPrivateKey(_container.GetHost(), SftpContainerFactory.Username, Path.Combine(_directory, "private.pem"), port: _container.GetPort(), remoteDirectory: directory),
            1 => factory.CreateSftpStorageWithPrivateKeyContent(_container.GetHost(), SftpContainerFactory.Username, _privateKey, port: _container.GetPort(), remoteDirectory: directory),
            _ => factory.CreateSftpStorageWithPassword(_container.GetHost(), SftpContainerFactory.Username, SftpContainerFactory.Password, _container.GetPort(), directory)
        };
        var token = TestContext.Current.CancellationToken;
        (await storage.CreateContainerAsync(token)).IsSuccess.ShouldBeTrue();
        var uploaded = await storage.UploadAsync("actual SSH authenticated bytes", new UploadOptions { Directory = "nested/child", FileName = "content.txt" }, token);
        uploaded.IsSuccess.ShouldBeTrue();
        var result = await storage.DownloadAsync("nested/child/content.txt", token);
        result.IsSuccess.ShouldBeTrue();
        await using (var file = result.Value!) (await File.ReadAllTextAsync(file.FilePath, token)).ShouldBe("actual SSH authenticated bytes");
        var defaults = (SftpStorageOptions)provider.GetServices<IStorageProvider>().Single(p => p is SftpStorageProvider).GetDefaultOptions();
        defaults.Host.ShouldBe(_container.GetHost());
        defaults.Password.ShouldBe(SftpContainerFactory.Password);
        (await storage.RemoveContainerAsync(token)).IsSuccess.ShouldBeTrue();
        ((SftpStorage)storage).StorageClient.Exists(directory).ShouldBeFalse();
        ((SftpStorage)storage).StorageClient.Dispose();
    }

    public async ValueTask DisposeAsync()
    {
        if (_container is not null) await _container.DisposeAsync();
        if (Directory.Exists(_directory)) Directory.Delete(_directory, true);
        GC.SuppressFinalize(this);
    }

    private static void WriteSshField(Stream stream, byte[] field)
    {
        Span<byte> length = stackalloc byte[4];
        BinaryPrimitives.WriteInt32BigEndian(length, field.Length);
        stream.Write(length);
        stream.Write(field);
    }
}
