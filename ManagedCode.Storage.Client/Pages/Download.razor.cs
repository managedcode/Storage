using ManagedCode.Storage.Client.Configurations;
using ManagedCode.Storage.Client.Services.Interfaces;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Options;
using Microsoft.JSInterop;

namespace ManagedCode.Storage.Client.Pages
{
    public partial class Download
    {
        [Inject]
        private IHttpClientService HttpClientService { get; set; } = default!;

        [Inject]
        private IOptions<AppSettings> AppSettings { get; set; } = default!;

        [Inject]
        private IJSRuntime JSRuntime { get; set; } = default!;

        private string fileName = null!;
        private string message = null!;
        private string result = null!;

        private async Task DownloadFile()
        {
            try
            {
                result = await HttpClientService.GetAsync<string>($"{AppSettings.Value.FileStorageUrl}/{fileName}");

                if (string.IsNullOrEmpty(result))
                {
                    message = "File not found or empty.";
                    result = string.Empty;

                    return;
                }

                message = "File content displayed.";
            }
            catch (Exception ex)
            {
                message = $"An error occurred: {ex.Message}";
                result = string.Empty;
            }
        }
    }
}