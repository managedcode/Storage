using System;
using System.Threading;
using System.Threading.Tasks;

namespace ManagedCode.Storage.Aws;

internal static class AsyncHelper
{
    private static readonly TaskFactory TaskFactory = new(CancellationToken.None,
        TaskCreationOptions.None,
        TaskContinuationOptions.None,
        TaskScheduler.Default);

    internal static void RunSync(Func<Task> task)
        => TaskFactory
            .StartNew(task)
            .Unwrap()
            .GetAwaiter()
            .GetResult();

    internal static TResult RunSync<TResult>(Func<Task<TResult>> task)
        => TaskFactory
            .StartNew(task)
            .Unwrap()
            .GetAwaiter()
            .GetResult();
}