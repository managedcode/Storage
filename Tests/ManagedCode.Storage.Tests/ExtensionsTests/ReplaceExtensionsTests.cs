using System.Threading.Tasks;
using Shouldly;
using ManagedCode.Storage.Azure;
using ManagedCode.Storage.Azure.Extensions;
using ManagedCode.Storage.Azure.Options;
using ManagedCode.Storage.Core;
using ManagedCode.Storage.TestFakes;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace ManagedCode.Storage.Tests.ExtensionsTests;

public class ReplaceExtensionsTests
{
    [Fact]
    public void ReplaceAzureStorageAsDefault()
    {
        var options = new AzureStorageOptions
        {
            Container = "test",
            ConnectionString = "ConnectionString"
        };

        var services = new ServiceCollection();

        services.AddAzureStorageAsDefault(options);

        services.ReplaceAzureStorageAsDefault();

        var build = services.BuildServiceProvider();
        build.GetService<IStorage>()
            !.GetType()
            .ShouldBe(typeof(FakeAzureStorage));
    }

    [Fact]
    public void ReplaceAzureStorage()
    {
        var options = new AzureStorageOptions
        {
            Container = "test",
            ConnectionString = "ConnectionString"
        };

        var services = new ServiceCollection();

        services.AddAzureStorage(options);

        services.ReplaceAzureStorage();

        var build = services.BuildServiceProvider();
        build.GetService<IAzureStorage>()
            !.GetType()
            .ShouldBe(typeof(FakeAzureStorage));
    }
}
