using System;
using ManagedCode.Storage.Aws.Builders;
using ManagedCode.Storage.Aws.Options;
using ManagedCode.Storage.Core.Builders;

namespace ManagedCode.Storage.Aws.Extensions
{
    public static class ProviderExtensions
    {
        public static AWSProviderBuilder AddAWSStorage(
            this ProviderBuilder providerBuilder,
            Action<AuthOptions> action)
        {
            var authOptions = new AuthOptions();
            action.Invoke(authOptions);

            return new AWSProviderBuilder(providerBuilder.ServiceCollection, authOptions);
        }
    }
}