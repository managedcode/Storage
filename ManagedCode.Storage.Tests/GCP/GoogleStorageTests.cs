using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Amazon.Runtime.Documents;
using FluentAssertions;
using Google.Apis.Auth.OAuth2;
using Google.Cloud.Storage.V1;
using ManagedCode.Storage.Core;
using ManagedCode.Storage.Gcp;
using ManagedCode.Storage.Gcp.Extensions;
using ManagedCode.Storage.Gcp.Options;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace ManagedCode.Storage.Tests.GCP;

public class GoogleStorageTests : StorageBaseTests
{
    protected override ServiceProvider ConfigureServices()
    {
        var services = new ServiceCollection();

        services.AddGCPStorageAsDefault(opt =>
        {
            //opt.GoogleCredential = GoogleCredential.FromFile("google_auth.json");
            opt.BucketOptions = new BucketOptions()
            {
                ProjectId = "api-project-0000000000000",
                Bucket = "managed-code-bucket",
            };
            opt.StorageClientBuilder = new StorageClientBuilder
            {
                UnauthenticatedAccess = true,
                BaseUri = "http://localhost:4443/storage/v1/",
            };
        });

        services.AddGCPStorage(new GCPStorageOptions
        {
            BucketOptions = new BucketOptions()
            {
                ProjectId = "api-project-0000000000000",
                Bucket = "managed-code-bucket",
            }
        });
        return services.BuildServiceProvider();
    }

    [Fact]
    public void StorageAsDefaultTest()
    {
        var storage = ServiceProvider.GetService<IGCPStorage>();
        var defaultStorage = ServiceProvider.GetService<IStorage>();
        storage?.GetType().FullName.Should().Be(defaultStorage?.GetType().FullName);
    }
}