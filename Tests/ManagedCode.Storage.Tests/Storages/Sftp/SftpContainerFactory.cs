using ManagedCode.Storage.Tests.Common;
using Testcontainers.Sftp;

namespace ManagedCode.Storage.Tests.Storages.Sftp;

internal static class SftpContainerFactory
{
    public const string Username = "storage";
    public const string Password = "storage-password";
    public const string UploadMountDirectory = "/upload";
    public const string RemoteDirectory = UploadMountDirectory + "/storage";

    public static SftpContainer Create()
    {
        return new SftpBuilder(ContainerImages.Sftp)
            .WithUsername(Username)
            .WithPassword(Password)
            .WithUploadDirectory(UploadMountDirectory)
            .WithCleanUp(true)
            .Build();
    }
}
