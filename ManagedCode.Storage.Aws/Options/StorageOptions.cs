namespace ManagedCode.Storage.Aws.Options
{
    public class StorageOptions
    {
        public string PublicKey { get; set; }
        public string SecretKey { get; set; }
        public string Bucket { get; set; }
        public string ServiceUrl { get; set; }
        public string ProfileName { get; set; }
        public string ServerSideEncryptionMethod { get; set; }
        public long ChunkedUploadThreshold { get; set; } = 100000000;
    }
}
