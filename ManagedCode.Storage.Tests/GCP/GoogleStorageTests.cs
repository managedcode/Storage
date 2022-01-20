using FluentAssertions;
using Google.Apis.Auth.OAuth2;
using ManagedCode.Storage.Core;
using ManagedCode.Storage.Gcp;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace ManagedCode.Storage.Tests.GCP
{
    public class GoogleStorageTests
    {
        private IBlobStorage _blobStorage;

        public GoogleStorageTests()
        {
            GoogleCredential googleCredential;
            using (Stream m = new FileStream("C:/myfiles/creds.json", FileMode.Open))
            {
                googleCredential = GoogleCredential.FromStream(m);
            }

            _blobStorage = new GoogleStorage(googleCredential, "my-dcs-1", "api-project-1073333651334");
        }

        [Fact]
        public async Task WhenUploadAsyncIsCalled()
        {
            var lineToUpload = "some crazy text";

            var byteArray = Encoding.ASCII.GetBytes(lineToUpload);
            var stream = new MemoryStream(byteArray);

            await _blobStorage.UploadAsync("b.txt", stream);
        }

        [Fact]
        public async Task WhenDownloadAsyncIsCalled()
        {
            var stream = await _blobStorage.DownloadAsStreamAsync("b.txt");
            stream.Seek(0, SeekOrigin.Begin);
            using var sr = new StreamReader(stream, Encoding.UTF8);

            string content = sr.ReadToEnd();

            content.Should().NotBeNull();
        }
    }
}
