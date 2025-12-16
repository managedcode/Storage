using ManagedCode.Storage.Core;
using ManagedCode.Storage.Core.Exceptions;
using ManagedCode.Storage.Dropbox;
using ManagedCode.Storage.Dropbox.Clients;
using ManagedCode.Storage.Dropbox.Extensions;
using ManagedCode.Storage.GoogleDrive;
using ManagedCode.Storage.GoogleDrive.Clients;
using ManagedCode.Storage.GoogleDrive.Extensions;
using ManagedCode.Storage.OneDrive;
using ManagedCode.Storage.OneDrive.Clients;
using ManagedCode.Storage.OneDrive.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Graph.Models;
using Shouldly;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using DriveFile = Google.Apis.Drive.v3.Data.File;

namespace ManagedCode.Storage.Tests.Storages.CloudDrive;

public class CloudDriveDependencyInjectionTests
{
    [Fact]
    public void Dropbox_AddAsDefault_ShouldResolveIStorage()
    {
        var services = new ServiceCollection();
        services.AddDropboxStorageAsDefault(options =>
        {
            options.RootPath = "/apps/demo";
            options.Client = new StubDropboxClient();
            options.CreateContainerIfNotExists = true;
        });

        using var provider = services.BuildServiceProvider();
        var storage = provider.GetRequiredService<IStorage>();
        var typed = provider.GetRequiredService<IDropboxStorage>();

        storage.ShouldBeSameAs(typed);
    }

    [Fact]
    public void Dropbox_AddAsDefault_Keyed_ShouldResolveKeyedIStorage()
    {
        var services = new ServiceCollection();
        services.AddDropboxStorageAsDefault("tenant-a", options =>
        {
            options.RootPath = "/apps/demo";
            options.Client = new StubDropboxClient();
        });

        using var provider = services.BuildServiceProvider();
        var storage = provider.GetRequiredKeyedService<IStorage>("tenant-a");
        var typed = provider.GetRequiredKeyedService<IDropboxStorage>("tenant-a");

        storage.ShouldBeSameAs(typed);
    }

    [Fact]
    public void Dropbox_WhenClientNotConfigured_ShouldThrow()
    {
        var services = new ServiceCollection();
        Should.Throw<BadConfigurationException>(() =>
            services.AddDropboxStorage(options => options.RootPath = "/apps/demo"));
    }

    [Fact]
    public void Dropbox_WhenAccessTokenConfigured_ShouldResolve()
    {
        var services = new ServiceCollection();
        services.AddDropboxStorage(options =>
        {
            options.RootPath = "/apps/demo";
            options.AccessToken = "test-token";
        });

        using var provider = services.BuildServiceProvider();
        provider.GetRequiredService<IDropboxStorage>().ShouldNotBeNull();
    }

    [Fact]
    public void Dropbox_WhenRefreshTokenMissingAppKey_ShouldThrow()
    {
        var services = new ServiceCollection();
        Should.Throw<BadConfigurationException>(() =>
            services.AddDropboxStorage(options =>
            {
                options.RootPath = "/apps/demo";
                options.RefreshToken = "refresh-token";
            }));
    }

    [Fact]
    public void Dropbox_WhenRefreshTokenConfigured_ShouldResolve()
    {
        var services = new ServiceCollection();
        services.AddDropboxStorage(options =>
        {
            options.RootPath = "/apps/demo";
            options.RefreshToken = "refresh-token";
            options.AppKey = "app-key";
        });

        using var provider = services.BuildServiceProvider();
        provider.GetRequiredService<IDropboxStorage>().ShouldNotBeNull();
    }

    [Fact]
    public void GoogleDrive_AddAsDefault_ShouldResolveIStorage()
    {
        var services = new ServiceCollection();
        services.AddGoogleDriveStorageAsDefault(options =>
        {
            options.RootFolderId = "root";
            options.Client = new StubGoogleDriveClient();
        });

        using var provider = services.BuildServiceProvider();
        var storage = provider.GetRequiredService<IStorage>();
        var typed = provider.GetRequiredService<IGoogleDriveStorage>();

        storage.ShouldBeSameAs(typed);
    }

    [Fact]
    public void GoogleDrive_AddAsDefault_Keyed_ShouldResolveKeyedIStorage()
    {
        var services = new ServiceCollection();
        services.AddGoogleDriveStorageAsDefault("tenant-a", options =>
        {
            options.RootFolderId = "root";
            options.Client = new StubGoogleDriveClient();
        });

        using var provider = services.BuildServiceProvider();
        var storage = provider.GetRequiredKeyedService<IStorage>("tenant-a");
        var typed = provider.GetRequiredKeyedService<IGoogleDriveStorage>("tenant-a");

        storage.ShouldBeSameAs(typed);
    }

    [Fact]
    public void GoogleDrive_WhenClientNotConfigured_ShouldThrow()
    {
        var services = new ServiceCollection();
        Should.Throw<BadConfigurationException>(() =>
            services.AddGoogleDriveStorage(options => options.RootFolderId = "root"));
    }

    [Fact]
    public void OneDrive_AddAsDefault_ShouldResolveIStorage()
    {
        var services = new ServiceCollection();
        services.AddOneDriveStorageAsDefault(options =>
        {
            options.DriveId = "me";
            options.RootPath = "demo";
            options.Client = new StubOneDriveClient();
        });

        using var provider = services.BuildServiceProvider();
        var storage = provider.GetRequiredService<IStorage>();
        var typed = provider.GetRequiredService<IOneDriveStorage>();

        storage.ShouldBeSameAs(typed);
    }

    [Fact]
    public void OneDrive_AddAsDefault_Keyed_ShouldResolveKeyedIStorage()
    {
        var services = new ServiceCollection();
        services.AddOneDriveStorageAsDefault("tenant-a", options =>
        {
            options.DriveId = "me";
            options.RootPath = "demo";
            options.Client = new StubOneDriveClient();
        });

        using var provider = services.BuildServiceProvider();
        var storage = provider.GetRequiredKeyedService<IStorage>("tenant-a");
        var typed = provider.GetRequiredKeyedService<IOneDriveStorage>("tenant-a");

        storage.ShouldBeSameAs(typed);
    }

    [Fact]
    public void OneDrive_WhenDriveIdMissing_ShouldThrow()
    {
        var services = new ServiceCollection();
        Should.Throw<BadConfigurationException>(() =>
            services.AddOneDriveStorage(options =>
            {
                options.Client = new StubOneDriveClient();
                options.DriveId = string.Empty;
            }));
    }

    private sealed class StubDropboxClient : IDropboxClientWrapper
    {
        public Task EnsureRootAsync(string rootPath, bool createIfNotExists, CancellationToken cancellationToken) => throw new NotImplementedException();

        public Task<DropboxItemMetadata> UploadAsync(string rootPath, string path, Stream content, string? contentType, CancellationToken cancellationToken)
            => throw new NotImplementedException();

        public Task<Stream> DownloadAsync(string rootPath, string path, CancellationToken cancellationToken) => throw new NotImplementedException();

        public Task<bool> DeleteAsync(string rootPath, string path, CancellationToken cancellationToken) => throw new NotImplementedException();

        public Task<bool> ExistsAsync(string rootPath, string path, CancellationToken cancellationToken) => throw new NotImplementedException();

        public Task<DropboxItemMetadata?> GetMetadataAsync(string rootPath, string path, CancellationToken cancellationToken) => throw new NotImplementedException();

        public IAsyncEnumerable<DropboxItemMetadata> ListAsync(string rootPath, string? directory, CancellationToken cancellationToken) => throw new NotImplementedException();
    }

    private sealed class StubGoogleDriveClient : IGoogleDriveClient
    {
        public Task EnsureRootAsync(string rootFolderId, bool createIfNotExists, CancellationToken cancellationToken) => throw new NotImplementedException();

        public Task<DriveFile> UploadAsync(string rootFolderId, string path, Stream content, string? contentType, bool supportsAllDrives, CancellationToken cancellationToken)
            => throw new NotImplementedException();

        public Task<Stream> DownloadAsync(string rootFolderId, string path, bool supportsAllDrives, CancellationToken cancellationToken) => throw new NotImplementedException();

        public Task<bool> DeleteAsync(string rootFolderId, string path, bool supportsAllDrives, CancellationToken cancellationToken) => throw new NotImplementedException();

        public Task<bool> ExistsAsync(string rootFolderId, string path, bool supportsAllDrives, CancellationToken cancellationToken) => throw new NotImplementedException();

        public Task<DriveFile?> GetMetadataAsync(string rootFolderId, string path, bool supportsAllDrives, CancellationToken cancellationToken) => throw new NotImplementedException();

        public IAsyncEnumerable<DriveFile> ListAsync(string rootFolderId, string? directory, bool supportsAllDrives, CancellationToken cancellationToken) => throw new NotImplementedException();
    }

    private sealed class StubOneDriveClient : IOneDriveClient
    {
        public Task EnsureRootAsync(string driveId, string rootPath, bool createIfNotExists, CancellationToken cancellationToken) => throw new NotImplementedException();

        public Task<DriveItem> UploadAsync(string driveId, string path, Stream content, string? contentType, CancellationToken cancellationToken)
            => throw new NotImplementedException();

        public Task<Stream> DownloadAsync(string driveId, string path, CancellationToken cancellationToken) => throw new NotImplementedException();

        public Task<bool> DeleteAsync(string driveId, string path, CancellationToken cancellationToken) => throw new NotImplementedException();

        public Task<bool> ExistsAsync(string driveId, string path, CancellationToken cancellationToken) => throw new NotImplementedException();

        public Task<DriveItem?> GetMetadataAsync(string driveId, string path, CancellationToken cancellationToken) => throw new NotImplementedException();

        public IAsyncEnumerable<DriveItem> ListAsync(string driveId, string? directory, CancellationToken cancellationToken) => throw new NotImplementedException();
    }
}
