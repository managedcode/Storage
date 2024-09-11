namespace ManagedCode.Storage.Client.Services.Interfaces
{
    public interface IHttpClientService
    {
        Task<TResponse> PostAsync<TResponse, TRequest>(string uri, TRequest body, string contentType);

        Task<TResponse> GetAsync<TResponse>(string uri);
    }
}