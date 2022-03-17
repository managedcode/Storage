using System;
using System.IO;
using System.Reflection;
using System.Text;
using Google.Apis.Auth.OAuth2;
using ManagedCode.Storage.Core.Builders;
using ManagedCode.Storage.Gcp.Builders;
using ManagedCode.Storage.Gcp.Options;

namespace ManagedCode.Storage.Gcp.Extensions;

public static class ProviderExtensions
{
    public static GoogleProviderBuilder AddGoogleStorage(
        this ProviderBuilder providerBuilder,
        Action<AuthFileNameOptions> action)
    {
        var fileNameOptions = new AuthFileNameOptions();
        action.Invoke(fileNameOptions);

        var path = Path.Combine(
            Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location),
            fileNameOptions.FileName
        );

        GoogleCredential googleCredential;
        using (Stream m = new FileStream(path, FileMode.Open))
        {
            googleCredential = GoogleCredential.FromStream(m);
        }

        return new GoogleProviderBuilder(providerBuilder.ServiceCollection, googleCredential);
    }

    public static GoogleProviderBuilder AddGoogleStorage(
        this ProviderBuilder providerBuilder,
        Action<AuthFileContentOptions> action)
    {
        var fileContentOptions = new AuthFileContentOptions();
        action.Invoke(fileContentOptions);

        GoogleCredential googleCredential;
        using (Stream m = new MemoryStream(Encoding.ASCII.GetBytes(fileContentOptions.Content)))
        {
            googleCredential = GoogleCredential.FromStream(m);
        }

        return new GoogleProviderBuilder(providerBuilder.ServiceCollection, googleCredential);
    }
}