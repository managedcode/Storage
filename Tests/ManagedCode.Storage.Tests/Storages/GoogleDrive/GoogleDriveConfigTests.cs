using ManagedCode.Storage.Core.Exceptions;
using ManagedCode.Storage.GoogleDrive.Extensions;
using ManagedCode.Storage.GoogleDrive.Options;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Xunit;

namespace ManagedCode.Storage.Tests.Storages.GoogleDrive;

/// <summary>
/// Configuration validation tests for Google Drive storage.
/// </summary>
public class GoogleDriveConfigTests
{
    [Fact]
    public void AddGoogleDriveStorage_WithoutCredentials_ShouldThrow()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act & Assert
        var exception = Should.Throw<BadConfigurationException>(() =>
        {
            services.AddGoogleDriveStorage(options =>
            {
                options.FolderId = "test-folder-id";
                // No credentials provided
            });
        });

        exception.Message.ShouldContain("Credential");
    }

    [Fact]
    public void AddGoogleDriveStorage_WithoutFolderId_ShouldThrow()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act & Assert
        var exception = Should.Throw<BadConfigurationException>(() =>
        {
            services.AddGoogleDriveStorage(options =>
            {
                options.ServiceAccountJson = GetFakeServiceAccountJson();
                // No FolderId provided
            });
        });

        exception.Message.ShouldContain("FolderId");
    }

    [Fact]
    public void AddGoogleDriveStorage_WithAllRequired_ShouldNotThrow()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act & Assert - Should not throw
        Should.NotThrow(() =>
        {
            services.AddGoogleDriveStorage(options =>
            {
                options.FolderId = "test-folder-id";
                options.ServiceAccountJson = GetFakeServiceAccountJson();
            });
        });
    }

    [Fact]
    public void GoogleDriveStorageOptions_DefaultValues_ShouldBeCorrect()
    {
        // Arrange & Act
        var options = new GoogleDriveStorageOptions();

        // Assert
        options.CreateContainerIfNotExists.ShouldBeTrue();
        options.ApplicationName.ShouldBe("ManagedCode.Storage.GoogleDrive");
        options.FolderId.ShouldBeNull();
        options.Credential.ShouldBeNull();
        options.ServiceAccountJson.ShouldBeNull();
        options.ServiceAccountJsonPath.ShouldBeNull();
    }

    [Fact]
    public void GoogleDriveStorageOptions_CanSetAllProperties()
    {
        // Arrange
        var options = new GoogleDriveStorageOptions();

        // Act
        options.FolderId = "folder-id";
        options.ApplicationName = "MyApp";
        options.CreateContainerIfNotExists = false;
        options.ServiceAccountJsonPath = "/path/to/key.json";

        // Assert
        options.FolderId.ShouldBe("folder-id");
        options.ApplicationName.ShouldBe("MyApp");
        options.CreateContainerIfNotExists.ShouldBeFalse();
        options.ServiceAccountJsonPath.ShouldBe("/path/to/key.json");
    }

    private static string GetFakeServiceAccountJson()
    {
        // This is a fake service account JSON for testing configuration validation
        // It won't work with real Google APIs
        return @"{
            ""type"": ""service_account"",
            ""project_id"": ""test-project"",
            ""private_key_id"": ""key123"",
            ""private_key"": ""-----BEGIN RSA PRIVATE KEY-----\nMIIEpAIBAAKCAQEA0Z3VS5JJcds3xfn/ygWyF/2L3B7p1LDzFCBMB8pEPUZmGdOu\n0b5JLnM5kPqvBWJlIZ6aMQ8c5xQ5LXZzh5BJ+LjuQxLqFvlYe6QFNNY0jZl1A3pR\n1M5VYQDJ5Z3hSl6LVJA7N4Z+3q3F0x4xFn7rKrnMnXR6vqgHU1gL3xgNchHBqKz7\n0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcde=\n-----END RSA PRIVATE KEY-----\n"",
            ""client_email"": ""test@test-project.iam.gserviceaccount.com"",
            ""client_id"": ""123456789"",
            ""auth_uri"": ""https://accounts.google.com/o/oauth2/auth"",
            ""token_uri"": ""https://oauth2.googleapis.com/token""
        }";
    }
}
