using Testcontainers.Sftp;

namespace ManagedCode.Storage.Tests.Storages.Sftp;

internal static class SftpContainerFactory
{
    public const string Username = "storage";
    public const string Password = "storage-password";
    public const string RemoteDirectory = "/upload";

    public static SftpContainer Create()
    {
        return new SftpBuilder()
            .WithUsername(Username)
            .WithPassword(Password)
            .WithUploadDirectory(RemoteDirectory)
            .WithCleanUp(true)
            .Build();
    }
}
