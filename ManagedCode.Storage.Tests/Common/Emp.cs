using System;
using System.Collections.Generic;
using System.Linq;
using DotNet.Testcontainers.Containers;
using Microsoft.Extensions.Logging;
using Testcontainers.Azurite;

namespace ManagedCode.Storage.Tests;

public sealed class EmptyContainer : DockerContainer
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AzuriteContainer" /> class.
    /// </summary>
    /// <param name="configuration">The container configuration.</param>
    /// <param name="logger">The logger.</param>
    public EmptyContainer(AzuriteConfiguration configuration, ILogger logger)
        : base(configuration, logger)
    {
    }
    
    public EmptyContainer() : base(null, null)
    {
    }

    /// <summary>
    /// Gets the Azurite connection string.
    /// </summary>
    /// <returns>The Azurite connection string.</returns>
    public string GetConnectionString()
    {
        return string.Empty;
    }
}