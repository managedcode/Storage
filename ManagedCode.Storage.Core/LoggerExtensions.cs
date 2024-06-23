using System;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;

namespace ManagedCode.Storage.Core;

public static class LoggerExtensions
{
    public static void LogException(this ILogger? logger, Exception ex, [CallerMemberName] string methodName = default!)
    {
        logger?.LogError(ex, methodName);
    }
}