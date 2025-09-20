using System.Threading.Tasks;
using FluentAssertions;
using ManagedCode.Storage.Core;
using ManagedCode.Storage.Core.Exceptions;
using ManagedCode.Storage.Sftp;
using ManagedCode.Storage.Sftp.Extensions;
using ManagedCode.Storage.Sftp.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace ManagedCode.Storage.Tests.Storages.Sftp;

public class SftpConfigTests
{
    [Fact]
    public void AddSftpStorage_WithPasswordAuth_ShouldSucceed()
    {
        var services = new ServiceCollection();

        var act = () => services.AddSftpStorage(options =>
        {
            options.Host = "localhost";
            options.Port = 22;
            options.Username = "tester";
            options.Password = "password";
        });

        act.Should().NotThrow();
    }

    [Fact]
    public void AddSftpStorage_WithKeyAuth_ShouldSucceed()
    {
        var services = new ServiceCollection();

        var act = () => services.AddSftpStorage(options =>
        {
            options.Host = "localhost";
            options.Port = 22;
            options.Username = "tester";
            options.PrivateKeyContent = "fake-key";
        });

        act.Should().NotThrow();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void AddSftpStorage_WithInvalidHost_ShouldThrow(string? host)
    {
        var services = new ServiceCollection();

        var act = () => services.AddSftpStorage(options =>
        {
            options.Host = host;
            options.Port = 22;
            options.Username = "tester";
            options.Password = "password";
        });

        act.Should().Throw<BadConfigurationException>().WithMessage("*host*");
    }

    [Fact]
    public void AddSftpStorage_WithInvalidPort_ShouldThrow()
    {
        var services = new ServiceCollection();

        var act = () => services.AddSftpStorage(options =>
        {
            options.Host = "localhost";
            options.Port = 0;
            options.Username = "tester";
            options.Password = "password";
        });

        act.Should().Throw<BadConfigurationException>().WithMessage("*port*");
    }

    [Fact]
    public void AddSftpStorage_WithoutCredentials_ShouldThrow()
    {
        var services = new ServiceCollection();

        var act = () => services.AddSftpStorage(options =>
        {
            options.Host = "localhost";
            options.Port = 22;
            options.Username = "tester";
        });

        act.Should().Throw<BadConfigurationException>().WithMessage("*credentials*");
    }

    [Fact]
    public void AddSftpStorageAsDefault_ShouldRegisterIStorage()
    {
        var services = new ServiceCollection();

        services.AddLogging();
        services.AddSftpStorageAsDefault(options =>
        {
            options.Host = "localhost";
            options.Port = 22;
            options.Username = "tester";
            options.Password = "password";
        });

        var provider = services.BuildServiceProvider();

        provider.GetRequiredService<ISftpStorage>().Should().NotBeNull();
        provider.GetRequiredService<IStorage>().Should().BeAssignableTo<ISftpStorage>();
    }

    [Fact]
    public void SftpStorageOptions_ShouldExposeDefaults()
    {
        var options = new SftpStorageOptions
        {
            Host = "localhost",
            Username = "tester",
            Password = "password"
        };

        using var storage = new SftpStorage(options, NullLogger<SftpStorage>.Instance);

        options.Port.Should().Be(22);
        options.RemoteDirectory.Should().Be("/");
        options.ConnectTimeout.Should().Be(15000);
        options.OperationTimeout.Should().Be(15000);
        options.CreateContainerIfNotExists.Should().BeTrue();
        options.CreateDirectoryIfNotExists.Should().BeTrue();
    }
}
