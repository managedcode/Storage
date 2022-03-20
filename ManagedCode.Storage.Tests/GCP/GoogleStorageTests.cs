using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Amazon.Runtime.Documents;
using FluentAssertions;
using Google.Apis.Auth.OAuth2;
using Google.Cloud.Storage.V1;
using ManagedCode.Storage.Gcp;
using ManagedCode.Storage.Gcp.Extensions;
using ManagedCode.Storage.Gcp.Options;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace ManagedCode.Storage.Tests.GCP;

public class GoogleStorageTests : StorageBaseTests
{
    public GoogleStorageTests()
    {
        var services = new ServiceCollection();

        services.AddGCPStorage(opt =>
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

        var provider = services.BuildServiceProvider();

        Storage = provider.GetService<IGCPStorage>();
    }
}
