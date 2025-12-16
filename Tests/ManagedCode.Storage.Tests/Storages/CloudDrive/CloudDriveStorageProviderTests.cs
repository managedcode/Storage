using ManagedCode.Storage.Dropbox;
using ManagedCode.Storage.Dropbox.Clients;
using ManagedCode.Storage.Dropbox.Options;
using ManagedCode.Storage.GoogleDrive;
using ManagedCode.Storage.GoogleDrive.Clients;
using ManagedCode.Storage.GoogleDrive.Options;
using ManagedCode.Storage.OneDrive;
using ManagedCode.Storage.OneDrive.Clients;
using ManagedCode.Storage.OneDrive.Options;
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

public class CloudDriveStorageProviderTests
{
    [Fact]
    public void DropboxStorageProvider_CreateStorage_ShouldUseDropboxOptions()
    {
        using var serviceProvider = new ServiceCollection().BuildServiceProvider();
        var defaultOptions = new DropboxStorageOptions
        {
            RootPath = "/apps/demo",
            Client = new StubDropboxClient(),
            CreateContainerIfNotExists = true
        };

        var provider = new DropboxStorageProvider(serviceProvider, defaultOptions);
        provider.StorageOptionsType.ShouldBe(typeof(DropboxStorageOptions));

        var cloned = provider.GetDefaultOptions().ShouldBeOfType<DropboxStorageOptions>();
        cloned.ShouldNotBeSameAs(defaultOptions);
        cloned.RootPath.ShouldBe(defaultOptions.RootPath);
        cloned.Client.ShouldBeSameAs(defaultOptions.Client);
        cloned.CreateContainerIfNotExists.ShouldBe(defaultOptions.CreateContainerIfNotExists);

        var storage = provider.CreateStorage<IDropboxStorage, DropboxStorageOptions>(cloned);
        storage.ShouldBeOfType<DropboxStorage>();
    }

    [Fact]
    public void DropboxStorageProvider_WhenOptionsWrong_ShouldThrow()
    {
        using var serviceProvider = new ServiceCollection().BuildServiceProvider();
        var provider = new DropboxStorageProvider(serviceProvider, new DropboxStorageOptions { RootPath = "/apps/demo", Client = new StubDropboxClient() });

        Should.Throw<ArgumentException>(() =>
            provider.CreateStorage<IDropboxStorage, GoogleDriveStorageOptions>(new GoogleDriveStorageOptions { Client = new StubGoogleDriveClient() }));
    }

    [Fact]
    public void GoogleDriveStorageProvider_CreateStorage_ShouldUseGoogleDriveOptions()
    {
        using var serviceProvider = new ServiceCollection().BuildServiceProvider();
        var defaultOptions = new GoogleDriveStorageOptions
        {
            RootFolderId = "root",
            Client = new StubGoogleDriveClient(),
            CreateContainerIfNotExists = true
        };

        var provider = new GoogleDriveStorageProvider(serviceProvider, defaultOptions);
        provider.StorageOptionsType.ShouldBe(typeof(GoogleDriveStorageOptions));

        var cloned = provider.GetDefaultOptions().ShouldBeOfType<GoogleDriveStorageOptions>();
        cloned.ShouldNotBeSameAs(defaultOptions);
        cloned.RootFolderId.ShouldBe(defaultOptions.RootFolderId);
        cloned.Client.ShouldBeSameAs(defaultOptions.Client);
        cloned.CreateContainerIfNotExists.ShouldBe(defaultOptions.CreateContainerIfNotExists);

        var storage = provider.CreateStorage<IGoogleDriveStorage, GoogleDriveStorageOptions>(cloned);
        storage.ShouldBeOfType<GoogleDriveStorage>();
    }

    [Fact]
    public void GoogleDriveStorageProvider_WhenOptionsWrong_ShouldThrow()
    {
        using var serviceProvider = new ServiceCollection().BuildServiceProvider();
        var provider = new GoogleDriveStorageProvider(serviceProvider, new GoogleDriveStorageOptions { RootFolderId = "root", Client = new StubGoogleDriveClient() });

        Should.Throw<ArgumentException>(() =>
            provider.CreateStorage<IGoogleDriveStorage, OneDriveStorageOptions>(new OneDriveStorageOptions { DriveId = "me", Client = new StubOneDriveClient() }));
    }

    [Fact]
    public void OneDriveStorageProvider_CreateStorage_ShouldUseOneDriveOptions()
    {
        using var serviceProvider = new ServiceCollection().BuildServiceProvider();
        var defaultOptions = new OneDriveStorageOptions
        {
            DriveId = "me",
            RootPath = "demo",
            Client = new StubOneDriveClient(),
            CreateContainerIfNotExists = true
        };

        var provider = new OneDriveStorageProvider(serviceProvider, defaultOptions);
        provider.StorageOptionsType.ShouldBe(typeof(OneDriveStorageOptions));

        var cloned = provider.GetDefaultOptions().ShouldBeOfType<OneDriveStorageOptions>();
        cloned.ShouldNotBeSameAs(defaultOptions);
        cloned.DriveId.ShouldBe(defaultOptions.DriveId);
        cloned.RootPath.ShouldBe(defaultOptions.RootPath);
        cloned.Client.ShouldBeSameAs(defaultOptions.Client);
        cloned.CreateContainerIfNotExists.ShouldBe(defaultOptions.CreateContainerIfNotExists);

        var storage = provider.CreateStorage<IOneDriveStorage, OneDriveStorageOptions>(cloned);
        storage.ShouldBeOfType<OneDriveStorage>();
    }

    [Fact]
    public void OneDriveStorageProvider_WhenOptionsWrong_ShouldThrow()
    {
        using var serviceProvider = new ServiceCollection().BuildServiceProvider();
        var provider = new OneDriveStorageProvider(serviceProvider, new OneDriveStorageOptions { DriveId = "me", RootPath = "demo", Client = new StubOneDriveClient() });

        Should.Throw<ArgumentException>(() =>
            provider.CreateStorage<IOneDriveStorage, DropboxStorageOptions>(new DropboxStorageOptions { RootPath = "/apps/demo", Client = new StubDropboxClient() }));
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
