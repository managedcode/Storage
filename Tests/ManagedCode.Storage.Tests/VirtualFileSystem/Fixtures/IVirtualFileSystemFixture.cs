using System.Threading.Tasks;

namespace ManagedCode.Storage.Tests.VirtualFileSystem.Fixtures;

public interface IVirtualFileSystemFixture
{
    Task<VirtualFileSystemTestContext> CreateContextAsync();
    VirtualFileSystemCapabilities Capabilities { get; }
}
