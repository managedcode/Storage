using DotNet.Testcontainers.Containers;
using ManagedCode.Storage.Tests.Common;

namespace ManagedCode.Storage.Tests.Storages;

public abstract class StreamTests<T> : BaseContainer<T> where T : IContainer
{
}