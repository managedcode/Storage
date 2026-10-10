using System;
using ManagedCode.Storage.Core;
using Orleans.Configuration;
using Orleans.Runtime;
using Shouldly;
using Xunit;

namespace ManagedCode.Storage.Tests.Storages.Orleans;

public sealed class OrleansConfigurationContractsTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public void InvalidProviderConfiguration_FailsWithItsSpecificReason(int invalid)
    {
        var options = new ManagedCodeStorageGrainStorageOptions();
        var name = "coverage";
        if (invalid == 0) name = " ";
        if (invalid == 1) options.StateDirectory = " ";
        if (invalid == 2) options.StorageServiceType = typeof(string);
        if (invalid == 3) options.StorageKey = " ";
        var exception = Should.Throw<OrleansConfigurationException>(() => new ManagedCodeStorageGrainStorageOptionsValidator(options, name).ValidateConfiguration());
        exception.Message.ShouldContain(invalid switch { 0 => "name", 1 => "StateDirectory", 2 => "StorageServiceType", _ => "StorageKey" });
    }

    [Fact]
    public void CustomPath_AllowsAnEmptyDefaultDirectory()
    {
        new ManagedCodeStorageGrainStorageOptionsValidator(new() { StateDirectory = "", PathBuilder = _ => "state.json", StorageServiceType = typeof(IStorage), StorageKey = "tenant" }, "coverage").ValidateConfiguration();
    }
}
