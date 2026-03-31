using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Playwright;
using Xunit;

namespace ManagedCode.Storage.Tests.Storages.Browser;

public abstract class BrowserPlaywrightHostFixtureBase(string relativeProjectPath, string databaseName, string containerName) : IAsyncLifetime
{
    private readonly StringBuilder _hostOutput = new();
    private Process? _hostProcess;
    private IPlaywright? _playwright;

    public string BaseUrl { get; private set; } = string.Empty;

    public IBrowser Browser { get; private set; } = null!;

    public string ContainerName { get; } = containerName;

    public string DatabaseName { get; } = databaseName;

    public string ContainerKey => $"managedcode.browser.indexeddb::{ContainerName}";

    public async Task InitializeAsync()
    {
        BaseUrl = $"http://127.0.0.1:{GetAvailablePort()}";
        _hostProcess = StartHostProcess(BaseUrl, relativeProjectPath);
        await WaitForHostAsync();

        _playwright = await Playwright.CreateAsync();
        Browser = await _playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Headless = true
        });
    }

    public async Task DisposeAsync()
    {
        if (Browser is not null)
            await Browser.DisposeAsync();

        _playwright?.Dispose();
        StopHostProcess();
    }

    public Task<IBrowserContext> CreateContextAsync()
    {
        return Browser.NewContextAsync(new BrowserNewContextOptions
        {
            BaseURL = BaseUrl
        });
    }

    private static int GetAvailablePort()
    {
        using var listener = new TcpListener(System.Net.IPAddress.Loopback, 0);
        listener.Start();
        return ((System.Net.IPEndPoint)listener.LocalEndpoint).Port;
    }

    private static string GetProjectPath(string relativeProjectPath)
    {
        var repositoryRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
        return Path.Combine(repositoryRoot, relativeProjectPath);
    }

    private Process StartHostProcess(string baseUrl, string relativeProjectPath)
    {
        var projectPath = GetProjectPath(relativeProjectPath);
        var workingDirectory = Path.GetDirectoryName(projectPath)
                               ?? throw new InvalidOperationException("Unable to resolve browser host working directory.");

        var startInfo = new ProcessStartInfo("dotnet")
        {
            Arguments = $"run --project \"{projectPath}\" --configuration Release --no-build --no-launch-profile",
            WorkingDirectory = workingDirectory,
            RedirectStandardError = true,
            RedirectStandardOutput = true,
            UseShellExecute = false
        };

        startInfo.Environment["ASPNETCORE_URLS"] = baseUrl;
        startInfo.Environment["DOTNET_ENVIRONMENT"] = "Development";

        var process = new Process
        {
            StartInfo = startInfo,
            EnableRaisingEvents = true
        };

        process.OutputDataReceived += AppendOutput;
        process.ErrorDataReceived += AppendOutput;

        if (!process.Start())
            throw new InvalidOperationException("Unable to start the browser Playwright host.");

        process.BeginOutputReadLine();
        process.BeginErrorReadLine();
        return process;
    }

    private void AppendOutput(object sender, DataReceivedEventArgs args)
    {
        if (string.IsNullOrWhiteSpace(args.Data))
            return;

        lock (_hostOutput)
        {
            _hostOutput.AppendLine(args.Data);
        }
    }

    private async Task WaitForHostAsync()
    {
        using var httpClient = new HttpClient
        {
            BaseAddress = new Uri(BaseUrl),
            Timeout = TimeSpan.FromSeconds(2)
        };

        var deadline = DateTimeOffset.UtcNow.AddSeconds(45);

        while (DateTimeOffset.UtcNow < deadline)
        {
            if (_hostProcess?.HasExited == true)
                throw new InvalidOperationException($"Browser host exited before becoming ready.{Environment.NewLine}{GetHostOutput()}");

            try
            {
                using var response = await httpClient.GetAsync("/storage-playground");
                if (response.IsSuccessStatusCode)
                    return;
            }
            catch (HttpRequestException)
            {
            }
            catch (TaskCanceledException)
            {
            }

            await Task.Delay(250);
        }

        throw new TimeoutException($"Timed out waiting for the browser Playwright host.{Environment.NewLine}{GetHostOutput()}");
    }

    private string GetHostOutput()
    {
        lock (_hostOutput)
        {
            return _hostOutput.ToString();
        }
    }

    private void StopHostProcess()
    {
        if (_hostProcess is null)
            return;

        if (!_hostProcess.HasExited)
            _hostProcess.Kill(entireProcessTree: true);

        _hostProcess.Dispose();
        _hostProcess = null;
    }
}
