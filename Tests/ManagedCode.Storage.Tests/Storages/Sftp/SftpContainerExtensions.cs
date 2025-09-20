using Testcontainers.Sftp;

namespace ManagedCode.Storage.Tests.Storages.Sftp;

internal static class SftpContainerExtensions
{
    public static string GetHost(this SftpContainer container)
    {
        return container.Hostname;
    }

    public static int GetPort(this SftpContainer container)
    {
        return container.GetMappedPublicPort();
    }
}
