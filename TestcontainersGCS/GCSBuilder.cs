using System.Text;
using System.Text.RegularExpressions;

namespace TestcontainersGCS;

/// <inheritdoc cref="ContainerBuilder{TBuilderEntity, TContainerEntity, TConfigurationEntity}" />
[PublicAPI]
public sealed class GCSBuilder : ContainerBuilder<GCSBuilder, GCSContainer, GCSConfiguration>
{
    public const string FakeGCSServerImage = "fsouza/fake-gcs-server:1.47.5";
    public const ushort FakeGCSServerPort = 4443;
    public const string StartupScriptFilePath = "/testcontainers.sh";

    /// <summary>
    /// Initializes a new instance of the <see cref="GCSBuilder" /> class.
    /// </summary>
    public GCSBuilder()
        : this(new GCSConfiguration())
    {
        DockerResourceConfiguration = Init().DockerResourceConfiguration;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="GCSBuilder" /> class.
    /// </summary>
    /// <param name="dockerResourceConfiguration">The Docker resource configuration.</param>
    private GCSBuilder(GCSConfiguration dockerResourceConfiguration)
        : base(dockerResourceConfiguration)
    {
        DockerResourceConfiguration = dockerResourceConfiguration;
    }

    /// <inheritdoc />
    protected override GCSConfiguration DockerResourceConfiguration { get; }

    /// <inheritdoc />
    public override GCSContainer Build()
    {
        Validate();
        return new GCSContainer(DockerResourceConfiguration, TestcontainersSettings.Logger);
    }

    /// <inheritdoc />
    protected override GCSBuilder Init()
    {
        return base.Init()
            .WithImage(FakeGCSServerImage)
            .WithPortBinding(FakeGCSServerPort, true)
            .WithEntrypoint("/bin/sh", "-c")
            .WithCommand($"while [ ! -f {StartupScriptFilePath} ]; do sleep 0.1; done; sh {StartupScriptFilePath}")
            .WithWaitStrategy(Wait.ForUnixContainer().UntilMessageIsLogged(new Regex("server started at.*", RegexOptions.IgnoreCase)))
            .WithStartupCallback((container, ct) =>
            {
                const char lf = '\n';
                var startupScript = new StringBuilder();
                startupScript.Append("#!/bin/bash");
                startupScript.Append(lf);
                startupScript.Append($"fake-gcs-server -backend memory -scheme http -port {FakeGCSServerPort} -external-url \"http://localhost:{container.GetMappedPublicPort(FakeGCSServerPort)}\"");
                startupScript.Append(lf);
                return container.CopyAsync(Encoding.Default.GetBytes(startupScript.ToString()), StartupScriptFilePath, Unix.FileMode755, ct);
            });
    }

    /// <inheritdoc />
    protected override GCSBuilder Clone(IContainerConfiguration resourceConfiguration)
    {
        return Merge(DockerResourceConfiguration, new GCSConfiguration(resourceConfiguration));
    }

    /// <inheritdoc />
    protected override GCSBuilder Clone(IResourceConfiguration<CreateContainerParameters> resourceConfiguration)
    {
        return Merge(DockerResourceConfiguration, new GCSConfiguration(resourceConfiguration));
    }

    /// <inheritdoc />
    protected override GCSBuilder Merge(GCSConfiguration oldValue, GCSConfiguration newValue)
    {
        return new GCSBuilder(new GCSConfiguration(oldValue, newValue));
    }
}