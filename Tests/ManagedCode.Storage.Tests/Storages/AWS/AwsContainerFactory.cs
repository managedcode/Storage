using System;
using System.Net;
using DotNet.Testcontainers.Builders;
using ManagedCode.Storage.Tests.Common;
using Testcontainers.LocalStack;

namespace ManagedCode.Storage.Tests.Storages.AWS;

internal static class AwsContainerFactory
{
    private const int EdgePort = 4566;

    public static LocalStackContainer Create()
    {
        return new LocalStackBuilder()
            .WithImage(ContainerImages.LocalStack)
            .WithEnvironment("SERVICES", "s3")
            .WithWaitStrategy(Wait.ForUnixContainer()
                .UntilHttpRequestIsSucceeded(request => request
                    .ForPort(EdgePort)
                    .ForPath("/_localstack/health")
                    .ForStatusCode(HttpStatusCode.OK),
                    wait => wait.WithTimeout(TimeSpan.FromMinutes(5))))
            .Build();
    }
}
