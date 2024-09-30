using ManagedCode.Communication;
using ManagedCode.Storage.Client;
using ManagedCode.Storage.Core.Helpers;
using ManagedCode.Storage.Core.Models;
using System.Net.Http;
using TestAssigmentClient.Services.Abstraction;

namespace TestAssigmentClient.Services
{
    public class BlobClientService : IBlobClientService
    {
        private readonly IStorageClient _storageClient;
        private readonly IConfiguration _configuration;

        public BlobClientService(HttpClient httpClient, IConfiguration configuration)
        {
            _configuration = configuration;
            _storageClient = new StorageClient(httpClient);

        }


        private const long LargeFileSize = 256 * 1024;
        private const long ChunkSize = 4096000;



        public async Task<Result<BlobMetadata>> UploadFileAsync(LocalFile file, CancellationToken cancellationToken = default)
        {
            var uploadUrl = _configuration["ApiRoutes:BaseUrl"] + _configuration["ApiRoutes:Upload"];
            var completeUrl = _configuration["ApiRoutes:BaseUrl"] + _configuration["ApiRoutes:Complete"];

            if (file.FileInfo.Length > LargeFileSize)
            {
                _storageClient.SetChunkSize(ChunkSize);

                var response = await _storageClient.UploadLargeFile(file: file.FileStream,
                   uploadApiUrl: uploadUrl,
                   completeApiUrl: completeUrl,
                   onProgressChanged: null,
                   cancellationToken: cancellationToken);

                if (response.IsSuccess)
                    return Result<BlobMetadata>.Succeed(file.BlobMetadata);

                return Result<BlobMetadata>.Fail(file.BlobMetadata);

            }
            else
            {
                return await _storageClient.UploadFile(file.FileStream, uploadUrl, "formFile");
            }

        }

        public async Task<Result<LocalFile>> DownloadFileAsync(string fileName, CancellationToken cancellationToken = default)
        {
            var downloadUrl = $"{_configuration["ApiRoutes:BaseUrl"]}{_configuration["ApiRoutes:Download"]}/{fileName}";

            return await _storageClient.DownloadFile(fileName, downloadUrl, null, cancellationToken);

        }


    }
}
