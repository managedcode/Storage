using System;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;

namespace ManagedCode.Storage.Core;

public static class LoggerExtensions
{
    private static readonly Action<ILogger, string, Exception?> LogExceptionMessage =
        LoggerMessage.Define<string>(
            LogLevel.Error,
            new EventId(0, nameof(LogException)),
            "Unhandled exception in {MethodName}");

    public static void LogException(this ILogger? logger, Exception ex, [CallerMemberName] string methodName = default!)
    {
        if (logger is null)
            return;

        LogExceptionMessage(logger, methodName, ex);
    }
}
