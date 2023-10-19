namespace ManagedCode.Storage.IntegrationTests.Constants;

public static class ApiEndpoints
{
    public const string Azure = "azure";
    
    public static class Base
    {
        public const string UploadFile = "{0}/upload";
        public const string UploadCreateFile = "{0}/upload-chunks/create";
        public const string UploadFileChunks = "{0}/upload-chunks";
        public const string UploadFileComplete = "{0}/upload-chunks/complete";
        public const string DownloadFile = "{0}/download";
    }
}