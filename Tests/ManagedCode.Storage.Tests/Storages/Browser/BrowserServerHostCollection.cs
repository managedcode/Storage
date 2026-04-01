using Xunit;

namespace ManagedCode.Storage.Tests.Storages.Browser;

[CollectionDefinition(nameof(BrowserServerHostCollection), DisableParallelization = true)]
public sealed class BrowserServerHostCollection : ICollectionFixture<BrowserServerHostFixture>
{
    public const string Name = nameof(BrowserServerHostCollection);
}
