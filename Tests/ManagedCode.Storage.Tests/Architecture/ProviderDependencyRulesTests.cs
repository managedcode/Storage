using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using ManagedCode.Storage.Aws;
using ManagedCode.Storage.Azure;
using ManagedCode.Storage.Azure.DataLake;
using ManagedCode.Storage.CloudKit;
using ManagedCode.Storage.Dropbox;
using ManagedCode.Storage.FileSystem;
using ManagedCode.Storage.Google;
using ManagedCode.Storage.GoogleDrive;
using ManagedCode.Storage.OneDrive;
using ManagedCode.Storage.Sftp;
using NetArchTest.Rules;
using Shouldly;
using Xunit;

namespace ManagedCode.Storage.Tests.Architecture;

public sealed class ProviderDependencyRulesTests
{
    [Theory]
    [MemberData(nameof(ProviderAssemblies))]
    public void ProviderAssemblies_ShouldNotDependOn_EachOther(Assembly assembly, string[] forbiddenAssemblyNames)
    {
        var referencedAssemblyNames = assembly.GetReferencedAssemblies()
            .Select(reference => reference.Name)
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Select(name => name!)
            .ToHashSet(StringComparer.Ordinal);

        var failedRules = forbiddenAssemblyNames
            .Where(referencedAssemblyNames.Contains)
            .ToArray();

        failedRules.ShouldBeEmpty(
            $"{assembly.GetName().Name} should not depend on other provider assemblies: {string.Join(", ", failedRules)}");
    }

    public static IEnumerable<object[]> ProviderAssemblies()
    {
        var providers = new[]
        {
            (typeof(IAWSStorage).Assembly, typeof(IAWSStorage).Assembly.GetName().Name!),
            (typeof(IAzureStorage).Assembly, typeof(IAzureStorage).Assembly.GetName().Name!),
            (typeof(IAzureDataLakeStorage).Assembly, typeof(IAzureDataLakeStorage).Assembly.GetName().Name!),
            (typeof(ICloudKitStorage).Assembly, typeof(ICloudKitStorage).Assembly.GetName().Name!),
            (typeof(IDropboxStorage).Assembly, typeof(IDropboxStorage).Assembly.GetName().Name!),
            (typeof(IFileSystemStorage).Assembly, typeof(IFileSystemStorage).Assembly.GetName().Name!),
            (typeof(IGCPStorage).Assembly, typeof(IGCPStorage).Assembly.GetName().Name!),
            (typeof(IGoogleDriveStorage).Assembly, typeof(IGoogleDriveStorage).Assembly.GetName().Name!),
            (typeof(IOneDriveStorage).Assembly, typeof(IOneDriveStorage).Assembly.GetName().Name!),
            (typeof(ISftpStorage).Assembly, typeof(ISftpStorage).Assembly.GetName().Name!)
        };

        foreach (var provider in providers)
        {
            yield return
                new object[]
                {
                    provider.Assembly,
                    providers
                        .Where(candidate => candidate.Assembly != provider.Assembly)
                        .Select(candidate => candidate.Item2)
                        .ToArray()
                };
        }
    }
}
