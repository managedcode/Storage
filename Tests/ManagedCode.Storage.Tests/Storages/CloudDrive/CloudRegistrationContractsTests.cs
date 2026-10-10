using System.Net.Http;
using System.Linq;
using Google.Apis.Drive.v3;
using Google.Apis.Services;
using ManagedCode.Storage.CloudKit;
using ManagedCode.Storage.CloudKit.Extensions;
using ManagedCode.Storage.CloudKit.Options;
using ManagedCode.Storage.Core;
using ManagedCode.Storage.Core.Providers;
using ManagedCode.Storage.Dropbox;
using ManagedCode.Storage.Dropbox.Extensions;
using ManagedCode.Storage.Dropbox.Options;
using ManagedCode.Storage.GoogleDrive;
using ManagedCode.Storage.GoogleDrive.Extensions;
using ManagedCode.Storage.GoogleDrive.Options;
using ManagedCode.Storage.OneDrive;
using ManagedCode.Storage.OneDrive.Extensions;
using ManagedCode.Storage.OneDrive.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Graph;
using Shouldly;
using Xunit;

namespace ManagedCode.Storage.Tests.Storages.CloudDrive;

public sealed class CloudRegistrationContractsTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Dropbox_KeyedTenantsAndDefaultOptionsRemainIndependent(bool asDefault)
    {
        var services = new ServiceCollection().AddLogging();
        foreach (var key in new[] { "a", "b" })
        {
            if (asDefault) services.AddDropboxStorageAsDefault(key, o => { o.AccessToken = "registration-only"; o.RootPath = "/" + key; });
            else services.AddDropboxStorage(key, o => { o.AccessToken = "registration-only"; o.RootPath = "/" + key; });
        }
        services.AddDropboxStorageAsDefault(new DropboxStorageOptions { AccessToken = "registration-only", RootPath = "/default" });
        using var provider = services.BuildServiceProvider();
        provider.GetRequiredKeyedService<IDropboxStorage>("a").ShouldNotBeSameAs(provider.GetRequiredKeyedService<IDropboxStorage>("b"));
        provider.GetRequiredKeyedService<DropboxStorageOptions>("a").RootPath.ShouldBe("/a");
        if (asDefault) provider.GetRequiredKeyedService<IStorage>("a").ShouldBeSameAs(provider.GetRequiredKeyedService<IDropboxStorage>("a"));
        var copy = (DropboxStorageOptions)provider.GetServices<IStorageProvider>().Single(p => p is DropboxStorageProvider).GetDefaultOptions();
        copy.RootPath = "/changed";
        provider.GetRequiredService<DropboxStorageOptions>().RootPath.ShouldBe("/default");
        copy.AccessToken.ShouldBe("registration-only");
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void GoogleDrive_KeyedTenantsAndDefaultOptionsPreserveTheSdkClient(bool asDefault)
    {
        using var drive = new DriveService(new BaseClientService.Initializer { ApplicationName = "Storage registration contracts" });
        var services = new ServiceCollection().AddLogging();
        foreach (var key in new[] { "a", "b" })
        {
            if (asDefault) services.AddGoogleDriveStorageAsDefault(key, o => { o.DriveService = drive; o.RootFolderId = key; });
            else services.AddGoogleDriveStorage(key, o => { o.DriveService = drive; o.RootFolderId = key; });
        }
        services.AddGoogleDriveStorageAsDefault(new GoogleDriveStorageOptions { DriveService = drive, RootFolderId = "default", SupportsAllDrives = true });
        using var provider = services.BuildServiceProvider();
        provider.GetRequiredKeyedService<IGoogleDriveStorage>("a").ShouldNotBeSameAs(provider.GetRequiredKeyedService<IGoogleDriveStorage>("b"));
        provider.GetRequiredKeyedService<GoogleDriveStorageOptions>("a").RootFolderId.ShouldBe("a");
        if (asDefault) provider.GetRequiredKeyedService<IStorage>("a").ShouldBeSameAs(provider.GetRequiredKeyedService<IGoogleDriveStorage>("a"));
        var copy = (GoogleDriveStorageOptions)provider.GetServices<IStorageProvider>().Single(p => p is GoogleDriveStorageProvider).GetDefaultOptions();
        copy.RootFolderId = "changed";
        provider.GetRequiredService<GoogleDriveStorageOptions>().RootFolderId.ShouldBe("default");
        copy.DriveService.ShouldBeSameAs(drive);
        copy.SupportsAllDrives.ShouldBeTrue();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void OneDrive_KeyedTenantsAndDefaultOptionsPreserveTheSdkClient(bool asDefault)
    {
        using var http = new HttpClient();
        var graph = new GraphServiceClient(http);
        var services = new ServiceCollection().AddLogging();
        foreach (var key in new[] { "a", "b" })
        {
            if (asDefault) services.AddOneDriveStorageAsDefault(key, o => { o.GraphClient = graph; o.DriveId = key; });
            else services.AddOneDriveStorage(key, o => { o.GraphClient = graph; o.DriveId = key; });
        }
        services.AddOneDriveStorageAsDefault(new OneDriveStorageOptions { GraphClient = graph, DriveId = "default" });
        using var provider = services.BuildServiceProvider();
        provider.GetRequiredKeyedService<IOneDriveStorage>("a").ShouldNotBeSameAs(provider.GetRequiredKeyedService<IOneDriveStorage>("b"));
        provider.GetRequiredKeyedService<OneDriveStorageOptions>("a").DriveId.ShouldBe("a");
        if (asDefault) provider.GetRequiredKeyedService<IStorage>("a").ShouldBeSameAs(provider.GetRequiredKeyedService<IOneDriveStorage>("a"));
        var copy = (OneDriveStorageOptions)provider.GetServices<IStorageProvider>().Single(p => p is OneDriveStorageProvider).GetDefaultOptions();
        copy.DriveId = "changed";
        provider.GetRequiredService<OneDriveStorageOptions>().DriveId.ShouldBe("default");
        copy.GraphClient.ShouldBeSameAs(graph);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void CloudKit_KeyedTenantsAndDefaultOptionsPreserveTheHttpClient(bool asDefault)
    {
        using var http = new HttpClient();
        var services = new ServiceCollection().AddLogging();
        foreach (var key in new[] { "a", "b" })
        {
            if (asDefault) services.AddCloudKitStorageAsDefault(key, o => { o.ContainerId = "iCloud.org.storage-tests"; o.ApiToken = "registration-only"; o.HttpClient = http; o.RootPath = key; });
            else services.AddCloudKitStorage(key, o => { o.ContainerId = "iCloud.org.storage-tests"; o.ApiToken = "registration-only"; o.HttpClient = http; o.RootPath = key; });
        }
        services.AddCloudKitStorageAsDefault(new CloudKitStorageOptions { ContainerId = "iCloud.org.storage-tests", ApiToken = "registration-only", HttpClient = http, RootPath = "default" });
        using var provider = services.BuildServiceProvider();
        provider.GetRequiredKeyedService<ICloudKitStorage>("a").ShouldNotBeSameAs(provider.GetRequiredKeyedService<ICloudKitStorage>("b"));
        provider.GetRequiredKeyedService<CloudKitStorageOptions>("a").RootPath.ShouldBe("a");
        if (asDefault) provider.GetRequiredKeyedService<IStorage>("a").ShouldBeSameAs(provider.GetRequiredKeyedService<ICloudKitStorage>("a"));
        var copy = (CloudKitStorageOptions)provider.GetServices<IStorageProvider>().Single(p => p is CloudKitStorageProvider).GetDefaultOptions();
        copy.RootPath = "changed";
        provider.GetRequiredService<CloudKitStorageOptions>().RootPath.ShouldBe("default");
        copy.HttpClient.ShouldBeSameAs(http);
    }
}
