// using ManagedCode.Storage.Azure.DataLake.Extensions;
// using Microsoft.Extensions.DependencyInjection;
//
// namespace ManagedCode.Storage.Tests.Azure;
//
// public class AzureDataLakeTests : StorageBaseTests
// {
//     protected override ServiceProvider ConfigureServices()
//     {
//         var services = new ServiceCollection();
//         services.AddLogging();
//
//         services.AddAzureDataLakeStorageAsDefault(opt =>
//         {
//             opt.FileSystem = "";
//
//             opt.ConnectionString =
//                 "";
//         });
//
//         return services.BuildServiceProvider();
//     }
// }

