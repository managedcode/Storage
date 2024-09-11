using ManagedCode.Storage.Client.Resources;
using System.Text;

namespace ManagedCode.Storage.Client.Services
{
    public class HttpClientService : IHttpClientService
    {
        private readonly IJsonSerializer _jsonSerializer;
        private readonly IOptions<AppSettings> _appSettings;
        private readonly IHttpClientFactory _httpClientFactory;

        public HttpClientService(
            IHttpClientFactory httpClientFactory,
            IOptions<AppSettings> appSettings,
            IJsonSerializer jsonSerializer)
        {
            _appSettings = appSettings;
            _jsonSerializer = jsonSerializer;
            _httpClientFactory = httpClientFactory;
        }

        public async Task<TResponse> PostAsync<TResponse, TRequest>(string uri, TRequest body, string contentType = ContentTypes.Json) =>
            await SendAsync<TResponse, TRequest>(uri, HttpMethod.Post, body, contentType);

        public async Task<TResponse> GetAsync<TResponse>(string uri) =>
            await SendAsync<TResponse, object?>(uri, HttpMethod.Get, null, ContentTypes.Json);

        private async Task<TResponse> SendAsync<TResponse, TRequest>(string uri, HttpMethod method, TRequest? content, string contentType)
        {
            var client = _httpClientFactory.CreateClient();
            client.Timeout = TimeSpan.FromMinutes(_appSettings.Value.RequestTimeoutInMinutes);

            var request = new HttpRequestMessage
            {
                RequestUri = new Uri(uri),
                Method = method
            };

            if (content != null)
            {
                switch (contentType)
                {
                    case ContentTypes.Json:
                        request.Content = new StringContent(_jsonSerializer.Serialize(content), Encoding.UTF8, contentType);
                        break;
                    case ContentTypes.MultipartFormData when content is MultipartFormDataContent multipartContent:
                        request.Content = multipartContent;
                        break;
                    default:
                        throw new InvalidOperationException(ErrorMessages.UnsupportedContentTypeOrMismatch);
                }
            }

            var response = await client.SendAsync(request);

            if (response.IsSuccessStatusCode && response.Content.Headers.ContentLength > 0)
            {
                var responseContent = await response.Content.ReadAsStringAsync();

                switch (response.Content.Headers.ContentType!.MediaType)
                {
                    case ContentTypes.Json:
                    case ContentTypes.MultipartFormData:
                        var deserializedResponse = _jsonSerializer.Deserialize<TResponse>(responseContent);
                        return deserializedResponse!;
                    case ContentTypes.TextPlain:
                        return (TResponse)(object)responseContent;
                    default:
                        throw new InvalidOperationException(ErrorMessages.UnsupportedContentTypeOrMismatch);
                }
            }

            throw new Exception($"Request failed with status code {(int)response.StatusCode}");
        }
    }
}