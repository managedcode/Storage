using ManagedCode.Storage.Client.Services;
using ManagedCode.Storage.FileSystem;
using ManagedCode.Storage.FileSystem.Options;

namespace ManagedCode.Storage.Client.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static void ConfigureOptions(this IServiceCollection serviceCollection, IConfiguration configuration)
        {
            serviceCollection.Configure<FileSystemStorageOptions>(configuration.GetSection(ConfigurationSectionsNames.FileSystemStorageOptions));
            serviceCollection.Configure<AppSettings>(configuration.GetSection(ConfigurationSectionsNames.AppSettings));
            serviceCollection.Configure<FormOptions>(configuration);
        }

        public static void AddServices(this IServiceCollection serviceCollection, ConfigurationManager configuration)
        {
            serviceCollection.AddHttpClient();

            serviceCollection.AddTransient<IJsonSerializer, JsonSerializer>();
            serviceCollection.AddTransient<IHttpClientService, HttpClientService>();

            // Register other storages if needed
            serviceCollection.AddTransient<IFileSystemStorage>(provider =>
            {
                var options = provider.GetRequiredService<IOptions<FileSystemStorageOptions>>().Value;

                return new FileSystemStorage(options);
            });
        }
    }
}