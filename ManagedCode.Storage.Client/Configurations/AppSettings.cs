namespace ManagedCode.Storage.Client.Configurations
{
    public class AppSettings
    {
        public string FileStorageUrl { get; set; } = null!;

        public int RequestTimeoutInMinutes { get; set; }

        public long MaxRequestBodySize { get; set; }
    }
}