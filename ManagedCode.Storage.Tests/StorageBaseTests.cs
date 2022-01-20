using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using ManagedCode.Storage.Core;

namespace ManagedCode.Storage.Tests
{
    public class StorageBaseTests
    {
        protected IBlobStorage _blobStorage;

        protected async Task SingleBlobExistsIsCalled(string fileName)
        {
            var result = await _blobStorage.ExistsAsync(fileName);

            result.Should().BeTrue();
        }

        protected void DIInitialized()
        {
            _blobStorage.Should().NotBeNull();
        }

        protected async Task DownloadAsyncIsCalled(string fileName)
        {
            var stream = await _blobStorage.DownloadAsStreamAsync(fileName);
            stream.Seek(0, SeekOrigin.Begin);
            using var sr = new StreamReader(stream, Encoding.UTF8);

            string content = sr.ReadToEnd();

            content.Should().NotBeNull();
        }

        protected async Task DownloadAsyncToFileIsCalled(string fileName)
        {
            string content = null;
            using (var tempFile = await _blobStorage.DownloadAsync(fileName))
            {
                using (var sr = new StreamReader(tempFile.FileStream, Encoding.UTF8))
                {
                    content = sr.ReadToEnd();
                }
            }

            content.Should().NotBeNull();
        }

        protected async Task UploadAsyncIsCalled(string fileName)
        {
            var lineToUpload = "some text";

            var byteArray = Encoding.ASCII.GetBytes(lineToUpload);
            var stream = new MemoryStream(byteArray);

            await _blobStorage.UploadStreamAsync(fileName, stream);
        }

        protected async Task DeleteAsyncIsCalled(string fileName)
        {
            await _blobStorage.DeleteAsync(fileName);
        }

        protected async Task GetBlobListAsyncIsCalled()
        {
            var aslist =  _blobStorage.GetBlobListAsync();
            var list = await aslist.ToListAsync(); // just for debug purposes
        }
    }
}
