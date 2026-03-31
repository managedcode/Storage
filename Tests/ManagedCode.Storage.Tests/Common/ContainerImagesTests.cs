using ManagedCode.Storage.Tests.Common;
using Shouldly;
using Xunit;

namespace ManagedCode.Storage.Tests.Common;

public sealed class ContainerImagesTests
{
    [Fact]
    public void LocalStack_ShouldUsePinnedPreAuthRelease()
    {
        ContainerImages.LocalStack.ShouldBe("localstack/localstack:4.14.0");
    }
}
