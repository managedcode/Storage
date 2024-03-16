using System.Net;
using System.Net.Http.Json;
using ManagedCode.Communication;
using ManagedCode.Storage.Core.Models;

namespace BlobStorageAccessClient;

/// <summary>
///     Class providing methods to interact with the blob storage API.
/// </summary>
public class ApiClient : IApiClient
{
    private readonly HttpClient _httpClient;

    /// <summary>
    ///     Initializes a new instance of the <see cref="ApiClient" /> class.
    /// </summary>
    /// <param name="httpClient">The HTTP client instance to perform requests.</param>
    public ApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _httpClient.BaseAddress = new Uri(ApiRoutes.BaseUrl);
    }

    /// <inheritdoc />
    public async Task<Result<BlobMetadata>> UploadFileAsync(LocalFile file,
        CancellationToken cancellationToken = default)
    {
        // Uploads a file to the server.

        // Creating content from the file stream.
        using var streamContent = new StreamContent(file.FileStream);
        using var formFile = new MultipartFormDataContent();
        formFile.Add(streamContent, "formFile", file.Name);

        // Sending a POST request to the server.
        using var response = await _httpClient.PostAsync(ApiRoutes.FileSystemUpload, formFile, cancellationToken);

        // Checking the success of the operation.
        if (response.IsSuccessStatusCode)
            return await response.Content.ReadFromJsonAsync<Result<BlobMetadata>>(cancellationToken);

        // Handling the error.
        var content = await response.Content.ReadAsStringAsync(cancellationToken);
        return Result<BlobMetadata>.Fail(response.StatusCode, content);
    }

    /// <inheritdoc />
    public async Task<Result<LocalFile>> DownloadFileAsync(string fileName,
        CancellationToken cancellationToken = default)
    {
        // Downloads a file from the server.
        try
        {
            // Executing a GET request to retrieve the file.
            await using var response =
                await _httpClient.GetStreamAsync($"{ApiRoutes.FileSystemDownload}/{fileName}", cancellationToken);
            var localFile = await LocalFile.FromStreamAsync(response, fileName);
            return Result<LocalFile>.Succeed(localFile);
        }
        catch (HttpRequestException e) when (e.StatusCode != null)
        {
            // Handling HTTP request error.
            return Result<LocalFile>.Fail(e.StatusCode.Value);
        }
        catch (Exception)
        {
            // Handling other errors.
            return Result<LocalFile>.Fail(HttpStatusCode.InternalServerError);
        }
    }

    /// <inheritdoc />
    public async Task<Result<bool>> DeleteFileAsync(string fileName, CancellationToken token = default)
    {
        // Deletes a file from the server.
        try
        {
            // Executing a DELETE request to delete the file.
            using var response = await _httpClient.DeleteAsync($"{ApiRoutes.FileSystemDelete}/{fileName}", token);
            return Result<bool>.Succeed(true);
        }
        catch (HttpRequestException e) when (e.StatusCode != null)
        {
            // Handling HTTP request error.
            return Result<bool>.Fail(e.StatusCode.Value);
        }
        catch (Exception)
        {
            // Handling other errors.
            return Result<bool>.Fail(HttpStatusCode.InternalServerError);
        }
    }
}