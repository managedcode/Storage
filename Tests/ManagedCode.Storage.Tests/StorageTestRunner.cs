using System.Threading.Tasks;
using Xunit.Runner.InProc.SystemConsole;

namespace ManagedCode.Storage.Tests;

internal static class StorageTestRunner
{
    public static async Task<int> Main(string[] args) => await ConsoleRunner.Run(args);
}
