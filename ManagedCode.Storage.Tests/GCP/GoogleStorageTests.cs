using System.IO;
using System.Threading.Tasks;
using Google.Apis.Auth.OAuth2;
using ManagedCode.Storage.Gcp;
using Xunit;

namespace ManagedCode.Storage.Tests.GCP
{
    public class GoogleStorageTests : StorageBaseTests
    {
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
        public void WhenDIInitialized()
        {
            DIInitialized();
        }

        [Fact]
        public async Task WhenSingleBlobExistsIsCalled()
        {
            await SingleBlobExistsIsCalled("a.txt");
        }

        [Fact]
        public async Task WhenDownloadAsyncIsCalled()
        {
            await DownloadAsyncIsCalled("a.txt");
        }

        [Fact]
        public async Task WhenDownloadAsyncToFileIsCalled()
        {
            await DownloadAsyncToFileIsCalled("a.txt");
        }

        [Fact]
        public async Task WhenUploadAsyncIsCalled()
        {
            await UploadAsyncIsCalled("b.txt");
        }

        [Fact]
        public async Task WhenDeleteAsyncIsCalled()
        {
            await DeleteAsyncIsCalled("a.txt");
        }

        [Fact]
        protected async Task WhenGetBlobListAsyncIsCalled()
        {
            await GetBlobListAsyncIsCalled();
        }
    }
}
