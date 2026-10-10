using System.Threading;
using System.Threading.Tasks;
using ManagedCode.Storage.Tests.VirtualFileSystem;
using Shouldly;
using Xunit;

namespace ManagedCode.Storage.Tests.Core;

public sealed class StorageContainerCancellationContractsTests
{
    [Fact]
    public async Task CanceledContainerCreation_ReturnsFailureAndPreservesTheCreationGate()
    {
        await using var session = await LocalVfsSession.CreateAsync();
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        var canceled = await session.Storage.CreateContainerAsync(cancellation.Token);
        canceled.IsFailed.ShouldBeTrue();
        var subsequent = await session.Storage.CreateContainerAsync(TestContext.Current.CancellationToken);
        subsequent.IsSuccess.ShouldBeTrue();
    }
}
