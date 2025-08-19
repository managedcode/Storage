using System.Threading.Tasks;
using FluentAssertions;
using ManagedCode.Storage.Core.Exceptions;
using ManagedCode.Storage.Ftp;
using ManagedCode.Storage.Ftp.Extensions;
using ManagedCode.Storage.Ftp.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace ManagedCode.Storage.Tests.Storages.Ftp;

/// <summary>
/// Configuration tests for FTP storage.
/// </summary>
public class FtpConfigTests
{
    [Fact]
    public void AddFtpStorage_WithValidOptions_ShouldSucceed()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        var act = () => services.AddFtpStorage(options =>
        {
            options.Host = "localhost";
            options.Port = 21;
            options.Username = "testuser";
            options.Password = "testpass";
        });

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void AddFtpStorage_WithEmptyHost_ShouldThrow()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        var act = () => services.AddFtpStorage(options =>
        {
            options.Host = "";
            options.Port = 21;
            options.Username = "testuser";
            options.Password = "testpass";
        });

        // Assert
        act.Should().Throw<BadConfigurationException>()
            .WithMessage("*Host*");
    }

    [Fact]
    public void AddFtpStorage_WithInvalidPort_ShouldThrow()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        var act = () => services.AddFtpStorage(options =>
        {
            options.Host = "localhost";
            options.Port = -1;
            options.Username = "testuser";
            options.Password = "testpass";
        });

        // Assert
        act.Should().Throw<BadConfigurationException>()
            .WithMessage("*Port*");
    }

    [Fact]
    public void AddFtpsStorage_WithValidOptions_ShouldSucceed()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        var act = () => services.AddFtpsStorage(options =>
        {
            options.Host = "localhost";
            options.Port = 990;
            options.Username = "testuser";
            options.Password = "testpass";
            options.EncryptionMode = FluentFTP.FtpEncryptionMode.Implicit;
        });

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void AddSftpStorage_WithValidPasswordAuth_ShouldSucceed()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        var act = () => services.AddSftpStorage(options =>
        {
            options.Host = "localhost";
            options.Port = 22;
            options.Username = "testuser";
            options.Password = "testpass";
        });

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void AddSftpStorage_WithValidKeyAuth_ShouldSucceed()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        var act = () => services.AddSftpStorage(options =>
        {
            options.Host = "localhost";
            options.Port = 22;
            options.Username = "testuser";
            options.PrivateKeyContent = "fake-private-key-content";
        });

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void AddSftpStorage_WithoutAuth_ShouldThrow()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        var act = () => services.AddSftpStorage(options =>
        {
            options.Host = "localhost";
            options.Port = 22;
            options.Username = "testuser";
            // No password or private key
        });

        // Assert
        act.Should().Throw<BadConfigurationException>()
            .WithMessage("*password or private key*");
    }

    [Fact]
    public async Task FtpStorage_Creation_ShouldHaveCorrectDefaults()
    {
        // Arrange
        var options = new FtpStorageOptions
        {
            Host = "localhost",
            Username = "testuser",
            Password = "testpass"
        };

        var storage = new FtpStorage(options, NullLogger<FtpStorage>.Instance);

        // Assert
        options.Port.Should().Be(21);
        options.RemoteDirectory.Should().Be("/");
        options.ConnectTimeout.Should().Be(15000);
        options.DataConnectionTimeout.Should().Be(15000);
        options.CreateContainerIfNotExists.Should().BeTrue();
        options.DataConnectionType.Should().Be(FluentFTP.FtpDataConnectionType.AutoPassive);
        
        // Cleanup
        storage.Dispose();
    }

    [Fact]
    public async Task FtpsStorage_Creation_ShouldHaveCorrectDefaults()
    {
        // Arrange
        var options = new FtpsStorageOptions
        {
            Host = "localhost",
            Username = "testuser",
            Password = "testpass"
        };

        var storage = new FtpStorage(options, NullLogger<FtpStorage>.Instance);

        // Assert
        options.Port.Should().Be(990);
        options.EncryptionMode.Should().Be(FluentFTP.FtpEncryptionMode.Implicit);
        options.ValidateAnyCertificate.Should().BeFalse();
        options.DataConnectionEncryption.Should().BeTrue();
        
        // Cleanup
        storage.Dispose();
    }

    [Fact(Skip = "SFTP support requires SSH.NET library integration which is not available in this build")]
    public async Task SftpStorage_Creation_ShouldHaveCorrectDefaults()
    {
        // Arrange
        var options = new SftpStorageOptions
        {
            Host = "localhost",
            Username = "testuser",
            Password = "testpass"
        };

        var storage = new FtpStorage(options, NullLogger<FtpStorage>.Instance);

        // Assert
        options.Port.Should().Be(22);
        options.AcceptAnyHostKey.Should().BeTrue();
        
        // Cleanup
        storage.Dispose();
    }
}