using Xunit;

namespace ManagedCode.Storage.Tests.Storages.Browser;

[CollectionDefinition(nameof(BrowserWasmHostCollection), DisableParallelization = true)]
public sealed class BrowserWasmHostCollection : ICollectionFixture<BrowserWasmHostFixture>
{
}
