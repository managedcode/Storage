using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using DotNet.Testcontainers.Configurations;
using Testcontainers.FakeGcsServer;

namespace ManagedCode.Storage.Tests.Common;

internal static class GcsContainerFactory
{
    private const string StagedScript = "/storage-start.partial";
    private const string ReadyScript = "/storage-start.sh";

    public static FakeGcsServerContainer Create(string backend = "memory")
    {
        if (backend is not ("memory" or "filesystem")) throw new ArgumentException("Unsupported GCS backend", nameof(backend));
        return new FakeGcsServerBuilder(ContainerImages.FakeGCSServer)
            .WithCommand(new OverwriteEnumerable<string>([$"while [ ! -f {ReadyScript} ]; do sleep 0.1; done; exec /bin/sh {ReadyScript}"]))
            .WithStartupCallback((container, token) => StageStartupAsync(container, backend, token))
            .Build();
    }

    private static async Task StageStartupAsync(FakeGcsServerContainer container, string backend, CancellationToken token)
    {
        // Publish only after CopyAsync has closed the file; executing during extraction causes ETXTBSY.
        var endpoint = new UriBuilder(Uri.UriSchemeHttp, container.Hostname,
            container.GetMappedPublicPort(FakeGcsServerBuilder.FakeGcsServerPort)).ToString().TrimEnd('/');
        var script = $"exec fake-gcs-server -backend {backend} -scheme http -external-url '{endpoint}'\n";
        await container.CopyAsync(Encoding.UTF8.GetBytes(script), StagedScript, ct: token);
        var result = await container.ExecAsync(["mv", StagedScript, ReadyScript], token);
        if (result.ExitCode != 0) throw new InvalidOperationException($"Failed to publish GCS startup script: {result.Stderr}");
    }
}
