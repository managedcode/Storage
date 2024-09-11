namespace ManagedCode.Storage.Client.Extensions
{
    public static class StorageFromFileExtensions
    {
        public static async Task<Result<BlobMetadata>> UploadToStorageAsync(this IStorage storage, Stream stream, UploadOptions options, CancellationToken cancellationToken = default)
        {
            return await storage.UploadAsync(stream, options, cancellationToken);
        }
    }
}