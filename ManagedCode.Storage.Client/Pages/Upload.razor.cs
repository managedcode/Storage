using ManagedCode.Communication;
using ManagedCode.Storage.Client.Configurations;
using ManagedCode.Storage.Client.Constants;
using ManagedCode.Storage.Client.Services.Interfaces;
using ManagedCode.Storage.Core.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.Extensions.Options;
using System.Net.Http.Headers;

namespace ManagedCode.Storage.Client.Pages
{
    public partial class Upload
    {
        [Inject]
        private IHttpClientService HttpClientService { get; set; } = default!;

        [Inject]
        private IOptions<AppSettings> AppSettings { get; set; } = default!;

        private IBrowserFile? selectedFile;
        private bool isLoading;
        private string? uploadMessage;

        private async Task OnInputFileChange(InputFileChangeEventArgs e)
        {
            isLoading = true;
            uploadMessage = null;
            selectedFile = null;

            var file = e.File;
            selectedFile = file;

            try
            {
                await UploadFile(file);

                uploadMessage = "File uploaded successfully!";
            }
            catch (Exception)
            {
                uploadMessage = $"Error uploading file: {file.Name}";
            }

            isLoading = false;
        }

        private async Task UploadFile(IBrowserFile file)
        {
            try
            {
                using var content = new MultipartFormDataContent();
                using var fileStream = file.OpenReadStream(AppSettings.Value.MaxRequestBodySize);

                var fileContent = new StreamContent(fileStream);
                fileContent.Headers.ContentType = new MediaTypeHeaderValue(file.ContentType);

                content.Add(fileContent, "file", file.Name);

                var result = await HttpClientService.PostAsync<Result<BlobMetadata>, MultipartFormDataContent>(AppSettings.Value.FileStorageUrl, content, ContentTypes.MultipartFormData);

                uploadMessage = "File uploaded successfully!";
            }
            catch (Exception ex)
            {
                uploadMessage = $"Error uploading file: {ex.Message}";

                throw;
            }
        }
    }
}