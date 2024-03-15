namespace ManagedCode.Storage.IntegrationTests.Constants;

public static class ApiEndpoints
{
    public const string Azure = "azure";
    public const string FileSystem = "fileSystem";
    
    public static class Base
    {
        public const string UploadFile = "{0}/upload";
        public const string UploadLargeFile = "{0}/upload-chunks";
        public const string DownloadFile = "{0}/download";
    }
}