using ManagedCode.Storage.TestFakes;
using Microsoft.AspNetCore.Mvc;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using WebApiSample.Controllers;
using Xunit;

namespace ManagedCode.Storage.IntegrationTests.Tests.AWS
{
    public class AWSStorageControllerTests
    {
        private readonly AWSStorageController _controller;
        private readonly FakeAWSStorage _fakeAwsStorage;

        public AWSStorageControllerTests()
        {
            _fakeAwsStorage = new FakeAWSStorage();
            _controller = new AWSStorageController(_fakeAwsStorage);
        }

        [Fact]
        public async Task Download_WhenFileExists_ReturnsFileContent()
        {
            var fileName = "testFile.txt";
            var fileContent = "Test content";

            var memoryStream = new MemoryStream(Encoding.UTF8.GetBytes(fileContent));

            var uploadResult = await _fakeAwsStorage.UploadAsync(memoryStream, options => 
            {
                options.FileName = fileName;
            });

            var result = await _controller.DownloadAsync(fileName);

            var fileResult = Assert.IsType<FileStreamResult>(result);
            var reader = new StreamReader(fileResult.FileStream);
            var downloadedContent = await reader.ReadToEndAsync();

            Assert.Equal(fileContent, downloadedContent);
        }

        [Fact]
        public async Task Download_WhenFileDoesNotExist_ReturnsNotFound()
        {
            var result = await _controller.DownloadAsync("nonexistentFile.txt");

            Assert.IsType<NotFoundObjectResult>(result);
        }

        [Fact]
        public async Task Upload_WhenFileIsProvided_ReturnsSuccess()
        {
            var fileName = "testUploadFile.txt";
            var fileContent = "Upload test content";
            var memoryStream = new MemoryStream(Encoding.UTF8.GetBytes(fileContent));

            var uploadResult = await _fakeAwsStorage.UploadAsync(memoryStream, options => options.FileName = fileName);

            Assert.True(uploadResult.IsSuccess);
        }

        [Fact]
        public async Task Delete_WhenFileExists_ReturnsSuccess()
        {
            var fileName = "testDeleteFile.txt";
            var fileContent = "Delete test content";
            var memoryStream = new MemoryStream(Encoding.UTF8.GetBytes(fileContent));

            await _fakeAwsStorage.UploadAsync(memoryStream, options => options.FileName = fileName);

            var result = await _controller.DeleteAsync(fileName);

            Assert.IsType<OkObjectResult>(result);
        }
    }
}
