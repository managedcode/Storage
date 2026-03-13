using System.Linq;
using System.Reflection;
using ManagedCode.Storage.Azure;
using ManagedCode.Storage.Client;
using ManagedCode.Storage.Client.SignalR;
using ManagedCode.Storage.Core;
using ManagedCode.Storage.Server.Controllers;
using ManagedCode.Storage.VirtualFileSystem.Core;
using NetArchTest.Rules;
using Orleans.Storage;
using Shouldly;
using Xunit;

namespace ManagedCode.Storage.Tests.Architecture;

public sealed class CoreAndProductionDependencyRulesTests
{
    [Fact]
    public void Core_ShouldNotDependOn_OuterLayers()
    {
        AssertHasNoDependencies(
            typeof(IStorage).Assembly,
            "ManagedCode.Storage.Aws",
            "ManagedCode.Storage.Azure",
            "ManagedCode.Storage.Azure.DataLake",
            "ManagedCode.Storage.CloudKit",
            "ManagedCode.Storage.Client",
            "ManagedCode.Storage.Client.SignalR",
            "ManagedCode.Storage.Dropbox",
            "ManagedCode.Storage.FileSystem",
            "ManagedCode.Storage.Google",
            "ManagedCode.Storage.GoogleDrive",
            "ManagedCode.Storage.OneDrive",
            "ManagedCode.Storage.Orleans",
            "ManagedCode.Storage.Server",
            "ManagedCode.Storage.Sftp",
            "ManagedCode.Storage.TestFakes",
            "ManagedCode.Storage.VirtualFileSystem");
    }

    [Theory]
    [MemberData(nameof(ProductionAssemblies))]
    public void ProductionAssemblies_ShouldNotDependOn_TestFakes(Assembly assembly)
    {
        AssertHasNoDependencies(assembly, "ManagedCode.Storage.TestFakes");
    }

    public static TheoryData<Assembly> ProductionAssemblies =>
        new()
        {
            typeof(IStorage).Assembly,
            typeof(StorageClient).Assembly,
            typeof(StorageSignalRClient).Assembly,
            typeof(StorageController).Assembly,
            typeof(ManagedCodeGrainStorage).Assembly,
            typeof(IVirtualFileSystem).Assembly,
            typeof(IAzureStorage).Assembly
        };

    private static void AssertHasNoDependencies(Assembly assembly, params string[] forbiddenNamespaces)
    {
        var failedRules = forbiddenNamespaces
            .Where(forbiddenNamespace => !Types.InAssembly(assembly)
                .ShouldNot()
                .HaveDependencyOn(forbiddenNamespace)
                .GetResult()
                .IsSuccessful)
            .ToArray();

        failedRules.ShouldBeEmpty(
            $"{assembly.GetName().Name} should not depend on: {string.Join(", ", failedRules)}");
    }
}
