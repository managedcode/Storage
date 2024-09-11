using ManagedCode.MimeTypes;

namespace ManagedCode.Storage.Client.Extensions
{
    public static class StorageExtensions
    {
        public static async Task<Result<FileResult>> DownloadAsFileResult(this IStorage storage, string blobName,
            CancellationToken cancellationToken = default)
        {
            var result = await storage.DownloadAsync(blobName, cancellationToken);

            if (result.IsFailed)
                return Result<FileResult>.Fail(result.Errors);

            var fileStream = new FileStreamResult(result.Value!.FileStream, MimeHelper.GetMimeType(result.Value.FileInfo.Extension))
            {
                FileDownloadName = result.Value.Name
            };

            return Result<FileResult>.Succeed(fileStream);
        }
    }
}