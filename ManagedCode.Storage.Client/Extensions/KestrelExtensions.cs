using ManagedCode.Storage.Client.Configurations;
using ManagedCode.Storage.Client.Constants;

namespace ManagedCode.Storage.Client.Extensions
{
    public static class KestrelExtensions
    {
        public static IWebHostBuilder ConfigureFileUploadLimit(this IWebHostBuilder webHostBuilder, IConfiguration configuration)
        {
            return webHostBuilder.ConfigureKestrel(options =>
            {
                var appSettings = configuration.GetSection(ConfigurationSectionsNames.AppSettings).Get<AppSettings>();

                options.Limits.MaxRequestBodySize = appSettings!.MaxRequestBodySize;
            });
        }
    }
}