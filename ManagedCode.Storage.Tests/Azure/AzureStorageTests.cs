using System.IO;
using System.Text;
using System.Threading.Tasks;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using FluentAssertions;
using ManagedCode.Storage.Azure;
using ManagedCode.Storage.Azure.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace ManagedCode.Storage.Tests.Azure;


public class AzureStorageTests : StorageBaseTests
{
    public AzureStorageTests()
    {
        var services = new ServiceCollection();

        services.AddAzureStorage(opt =>
        {
            opt.Container = "managed-code-bucket";
            //https://github.com/marketplace/actions/azuright
            opt.ConnectionString =
                //"DefaultEndpointsProtocol=http;AccountName=devstoreaccount1;AccountKey=Eby8vdM02xNOcqFlqUwJPLlmEtlCDXJ1OUzFT50uSRZ6IFsuFq2UVErCz4I6tq/K1SZFPTOtr/KBHBeksoGMGw==;BlobEndpoint=http://localhost:10000/devstoreaccount1;QueueEndpoint=http://localhost:10001/devstoreaccount1;TableEndpoint=http://localhost:10002/devstoreaccount1;";
                "DefaultEndpointsProtocol=https;AccountName=winktdev;AccountKey=F7F9vhS+SxgY8b0/mrGYZCV6QOoKwv8FqAHsDN/aZC4OPeyPhHS8OKRi3Uc9VIHcel5+oweEmRQs4Be+r0pFMg==;EndpointSuffix=core.windows.net";
                    });

        var provider = services.BuildServiceProvider();

        Storage = provider.GetService<IAzureStorage>();
    }
    
}