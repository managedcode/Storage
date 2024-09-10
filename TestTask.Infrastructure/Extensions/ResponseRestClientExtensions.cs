using System.Net;
using System.Text.Json;
using ManagedCode.Communication;
using Microsoft.Extensions.Logging;
using RestSharp;

namespace TestTask.Infrastructure.Extensions;

public static class ResponseRestClientExtensions
    {
        public async static Task<Result<T>> HandleRestClientResponse<T>(this RestClient client, RestRequest request, ILogger logger, CancellationToken cancellationToken = default)
        {
            try
            {
                var response = await client.ExecuteAsync(request, cancellationToken);
                if (response.IsSuccessful)
                {
                    if (response.Content is not null)
                    {
                        var result = JsonSerializer.Deserialize<T>(response.Content);

                        if (result is not null)
                        {
                            return Result<T>.Succeed(result);
                        }

                        logger.LogWarning("Successful response, but could not deserialize content.");
                        return Result<T>.Fail("Could not deserialize the server response.");
                    }

                    logger.LogWarning("Successful response, but no content returned.");
                    return Result<T>.Fail("The server returned no content.");
                }
                switch (response.StatusCode)
                {
                    case HttpStatusCode.BadRequest:
                        logger.LogError("Bad Request: {0}", response.Content);
                        return Result<T>.Fail("Bad Request: The request failed due to invalid input.");

                    case HttpStatusCode.InternalServerError:
                        logger.LogError("Server error: {0}", response.Content);
                        return Result<T>.Fail("Internal Server Error: The server encountered an error while processing the request.");

                    default:
                        logger.LogError("Unexpected error occurred: {0}", response.Content);
                        return Result<T>.Fail($"Unexpected Error: {response.StatusCode}, {response.Content}");
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An error occurred while processing the request.");
                return Result<T>.Fail("An unexpected error occurred during the request.");
            }
        }
    }