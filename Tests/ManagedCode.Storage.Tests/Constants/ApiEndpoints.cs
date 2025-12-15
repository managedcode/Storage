namespace ManagedCode.Storage.Tests.Constants;

public static class ApiEndpoints
{
    public const string Azure = "azure";

    public static class Base
    {
        public const string UploadFile = "{0}/upload";
        public const string DownloadFile = "{0}/download/{1}";
        public const string DownloadBytes = "{0}/download-bytes/{1}";
        public const string StreamFile = "{0}/stream/{1}";
        public const string UploadLargeFile = "{0}/upload-chunks";
    }
}
