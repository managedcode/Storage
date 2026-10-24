using System;
using Azure;
using ManagedCode.Communication;

namespace ManagedCode.Storage.Azure;

public partial class AzureStorage
{
    private const int MinimumHttpErrorStatus = 400;
    private const int MaximumHttpErrorStatus = 599;

    private static Problem ProviderFailure(Exception exception)
    {
        if (exception is not RequestFailedException { Status: >= MinimumHttpErrorStatus and <= MaximumHttpErrorStatus } failure)
        {
            return Problem.FromException(exception);
        }

        var problem = Problem.FromException(failure, failure.Status);
        if (!string.IsNullOrEmpty(failure.ErrorCode))
        {
            problem.ErrorCode = failure.ErrorCode;
        }
        return problem;
    }
}
