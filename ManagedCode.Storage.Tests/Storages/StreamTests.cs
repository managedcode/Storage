using System.Threading.Tasks;
using DotNet.Testcontainers.Containers;
using ManagedCode.Storage.Tests.Common;
using Xunit;

namespace ManagedCode.Storage.Tests.Storages;

public abstract class StreamTests<T> : BaseContainer<T>
    where T : IContainer
{
    
}