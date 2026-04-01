using Xunit;

namespace ManagedCode.Storage.Tests.Storages.Browser;

[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class BrowserIntegrationCollection : ICollectionFixture<BrowserServerHostFixture>, ICollectionFixture<BrowserWasmHostFixture>
{
    public const string Name = nameof(BrowserIntegrationCollection);
}
